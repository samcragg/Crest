// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System;
    using System.Collections.Generic;
    using Crest.Abstractions;

    /// <summary>
    /// Represents configuration options used during the validation of JWTs.
    /// </summary>
    public class JwtValidationSettings : IJwtSettings
    {
        /// <summary>
        /// Gets the claim property the original JWT claim name is stored
        /// against if a match is found in the <see cref="JwtClaimMappings"/>
        /// dictionary.
        /// </summary>
        public const string JwtClaimProperty = "http://schemas.xmlsoap.org/ws/2005/05/identity/claimproperties/ShortTypeName";

        // RFC 7519 states the audience/issuer values are case-sensitive
        private readonly HashSet<string> audiences = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> issuers = new HashSet<string>(StringComparer.Ordinal);

        private readonly Dictionary<string, string> mappings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "actort", "http://schemas.xmlsoap.org/ws/2009/09/identity/claims/actor" },
            { "birthdate", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dateofbirth" },
            { "commonname", "http://schemas.xmlsoap.org/claims/CommonName" },
            { "email", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress" },
            { "family_name", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname" },
            { "gender", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/gender" },
            { "given_name", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname" },
            { "group", "http://schemas.xmlsoap.org/claims/Group" },
            { "nameid", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" },
            { "ppid", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/privatepersonalidentifier" },
            { "sub", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" },
            { "unique_name", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" },
            { "upn", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn" },
            { "website", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/webpage" }
        };

        /// <inheritdoc />
        public ISet<string> Audiences => this.audiences;

        /// <inheritdoc />
        public virtual string AuthenticationType => "AuthenticationTypes.Federation";

        /// <summary>
        /// Gets or sets the clock skew to apply when validating a time.
        /// </summary>
        /// <remarks>
        /// Defaults to five minutes.
        /// </remarks>
        public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

        /// <inheritdoc />
        public ISet<string> Issuers => this.issuers;

        /// <inheritdoc />
        public virtual bool SkipAuthentication => false;

        /// <inheritdoc />
        IReadOnlyDictionary<string, string> IJwtSettings.JwtClaimMappings => this.mappings;

        /// <summary>
        /// Gets the mappings between a JWT and a <see cref="System.Security.Claims.Claim"/>.
        /// </summary>
        protected internal IDictionary<string, string> JwtClaimMappings => this.mappings;
    }
}
