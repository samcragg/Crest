// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using Crest.Host.Diagnostics;
    using Crest.Host.Logging;

    /// <summary>
    /// Allows the validation of JWTs.
    /// </summary>
    internal partial class JwtValidator
    {
        private const string JwtClaimProperty = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/ShortTypeName";
        private static readonly ILog Logger = LogProvider.For<JwtValidator>();
        private static readonly DateTime UnixEpoc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly JwtValidationSettings settings;
        private readonly ITimeProvider timeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtValidator"/> class.
        /// </summary>
        /// <param name="timeProvider">Provides the current time.</param>
        /// <param name="settings">The settings to use during validation.</param>
        public JwtValidator(ITimeProvider timeProvider, JwtValidationSettings settings)
        {
            this.settings = settings;
            this.timeProvider = timeProvider;
        }

        /// <summary>
        /// Validate the specified data and converts it to a claims principal
        /// if the validation succeeds; otherwise, returns <c>null</c>.
        /// </summary>
        /// <param name="payload">The payload of the JWT.</param>
        /// <returns>
        /// The converted claims principal if validation succeeds; otherwise,
        /// <c>null</c>.
        /// </returns>
        public virtual ClaimsPrincipal GetValidClaimsPrincipal(byte[] payload)
        {
            var registered = new RegisteredClaims();
            ClaimsPrincipal principal = this.ConvertToClaimsPrincipal(payload, registered);

            if (!this.IsValidIssuer(registered.Iss))
            {
                return null;
            }

            if (!this.IsValidAudience(registered.Aud))
            {
                return null;
            }

            if (!this.IsValidExpiration(registered.Exp))
            {
                return null;
            }

            if (!this.IsValidNotBefore(registered.Nbf))
            {
                return null;
            }

            return principal;
        }

        private static DateTime ConvertSecondsSinceEpoch(string value, DateTime invalidValue)
        {
            if (long.TryParse(value, out long seconds))
            {
                return UnixEpoc.AddSeconds(seconds);
            }
            else
            {
                Logger.WarnFormat("Unable to parse time-stamp '{value}'", value);
                return invalidValue;
            }
        }

        private static IEnumerable<Claim> ParseClaims(
            IDictionary<string, string> mappings,
            byte[] payload,
            RegisteredClaims knownClaims)
        {
            using (var parser = new JsonObjectParser(payload))
            {
                foreach (KeyValuePair<string, string> kvp in parser.GetPairs())
                {
                    knownClaims.SetClaim(kvp.Key, kvp.Value);
                    if (mappings.TryGetValue(kvp.Key, out string mapped))
                    {
                        var claim = new Claim(mapped, kvp.Value);
                        claim.Properties.Add(JwtClaimProperty, kvp.Key);
                        yield return claim;
                    }
                    else
                    {
                        yield return new Claim(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        private ClaimsPrincipal ConvertToClaimsPrincipal(byte[] payload, RegisteredClaims knownClaims)
        {
            return new ClaimsPrincipal(
                new ClaimsIdentity(
                    ParseClaims(this.settings.JwtClaimMappings, payload, knownClaims),
                    this.settings.AuthenticationType));
        }

        private bool IsValidAudience(string[] aud)
        {
            if (aud == null)
            {
                return true;
            }

            if (this.settings.Audiences.Overlaps(aud))
            {
                return true;
            }
            else
            {
                Logger.InfoFormat("Unknown JWT aud claim value '{aud}'.", aud);
                return false;
            }
        }

        private bool IsValidExpiration(string exp)
        {
            if (string.IsNullOrEmpty(exp))
            {
                return true;
            }

            // If the JWT says it expires 10:00 but our clock thinks it is
            // 10:04, subtract the clock skew of five minutes so that the
            // current time becomes 09:59 and, therefore, valid.
            //
            // RFC 7519 § 4.1.4
            // The processing of the "exp" claim requires that the current
            // date/time MUST be before the expiration date/time listed in the
            // "exp" claim.
            DateTime expire = ConvertSecondsSinceEpoch(exp, DateTime.MinValue);
            DateTime now = this.timeProvider.GetUtc().Subtract(this.settings.ClockSkew);
            if (now < expire)
            {
                return true;
            }
            else
            {
                Logger.InfoFormat(
                    "JWT exp claim is too late ({exp} <= {time}). Using clock scew of {scew}.",
                    expire,
                    now,
                    this.settings.ClockSkew);

                return false;
            }
        }

        private bool IsValidIssuer(string iss)
        {
            if (string.IsNullOrEmpty(iss) || this.settings.Issuers.Contains(iss))
            {
                return true;
            }
            else
            {
                Logger.InfoFormat("Unknown JWT iss claim value '{iss}'.", iss);
                return false;
            }
        }

        private bool IsValidNotBefore(string nbf)
        {
            if (string.IsNullOrEmpty(nbf))
            {
                return true;
            }

            // If the JWT says it can't be used before 10:00 but our clock
            // thinks it is 9:55, add the clock skew of five minutes so that
            // the current time becomes 10:00 and, therefore, valid.
            //
            // RFC 7519 § 4.1.4
            // The processing of the "nbf" claim requires that the current
            // date/time MUST be after or equal to the not-before date/time
            // listed in the "nbf" claim.
            DateTime notBefore = ConvertSecondsSinceEpoch(nbf, DateTime.MaxValue);
            DateTime now = this.timeProvider.GetUtc().Add(this.settings.ClockSkew);
            if (now >= notBefore)
            {
                return true;
            }
            else
            {
                Logger.InfoFormat(
                    "JWT nbf claim is too early ({nbf} > {time} - time includes clock scew of {scew}).",
                    notBefore,
                    now,
                    this.settings.ClockSkew);

                return false;
            }
        }
    }
}
