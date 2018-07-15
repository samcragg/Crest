// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using Crest.Host.IO;
    using Crest.Host.Logging;

    /// <summary>
    /// Allows the verification of the JWT signature.
    /// </summary>
    internal partial class JwtSignatureVerifier
    {
        private static readonly ILog Logger = LogProvider.For<JwtSignatureVerifier>();
        private readonly ISignatureValidator[] validators;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtSignatureVerifier"/> class.
        /// </summary>
        /// <param name="validators">The objects to use to validate a signature.</param>
        public JwtSignatureVerifier(ISignatureValidator[] validators)
        {
            this.validators = validators;
        }

        /// <summary>
        /// Determines whether the specified JWT token is authentic or not.
        /// </summary>
        /// <param name="token">The JWT token string.</param>
        /// <param name="payload">When this method returns, contains the payload of.</param>
        /// <returns>
        /// <c>true</c> if token has been validly signed; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Only the signature is checked to determine whether the contents
        /// have been signed by a valid certificate/secret key - no additional
        /// validation (such as the contents of the body is valid JSON, if the
        /// token has expired etc) are performed.
        /// </remarks>
        public virtual bool IsSignatureValid(string token, out byte[] payload)
        {
            payload = null;
            JwtInformation jwt = SplitToken(token);
            if (jwt == null)
            {
                return false;
            }

            if (!ReadHeaderInformation(jwt, token))
            {
                return false;
            }

            if (!this.GetValidator(jwt))
            {
                return false;
            }

            byte[] data = CopyBytes(token, jwt.SignatureStart);
            if (!UrlBase64.TryDecode(token, jwt.SignatureStart + 1, token.Length, out byte[] signature))
            {
                return false;
            }

            if (!jwt.Validator.IsValid(data, signature, jwt.AlgorithmName))
            {
                return false;
            }

            return UrlBase64.TryDecode(token, jwt.PayloadStart + 1, jwt.SignatureStart, out payload);
        }

        private static byte[] CopyBytes(string source, int length)
        {
            var bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = (byte)source[i];
            }

            return bytes;
        }

        private static HashAlgorithmName GetHashingAlgorithm(string algClaim)
        {
            switch (algClaim.Substring(2))
            {
                case "256":
                    return HashAlgorithmName.SHA256;

                case "384":
                    return HashAlgorithmName.SHA384;

                case "512":
                    return HashAlgorithmName.SHA512;

                default:
                    return default;
            }
        }

        private static bool ReadHeaderInformation(JwtInformation jwt, string token)
        {
            if (!UrlBase64.TryDecode(token, 0, jwt.PayloadStart, out byte[] headerBytes))
            {
                return false;
            }

            using (var parser = new JsonObjectParser(headerBytes))
            {
                foreach (KeyValuePair<string, string> kvp in parser.GetPairs())
                {
                    jwt.SetProperty(kvp.Key, kvp.Value);
                }
            }

            // RFC 7519:
            //    While media type names are not case sensitive, it is
            //    RECOMMENDED that "JWT" always be spelled using uppercase
            //    characters
            // Hence the need to use case insensitive comparison
            if ((jwt.Typ == null) ||
                string.Equals(jwt.Typ, "JWT", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                Logger.InfoFormat("Unknown JWT type: '{typ}'", jwt.Typ);
                return false;
            }
        }

        private static JwtInformation SplitToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Logger.Info("Empty JWT token");
                return null;
            }

            int payloadStart = token.IndexOf('.');
            if (payloadStart < 0)
            {
                Logger.Info("JWT missing payload separator");
                return null;
            }

            int signatureStart = token.IndexOf('.', payloadStart + 1);
            if (signatureStart < 0)
            {
                Logger.Info("JWT missing signature separator");
                return null;
            }

            return new JwtInformation
            {
                PayloadStart = payloadStart,
                SignatureStart = signatureStart,
            };
        }

        private ISignatureValidator FindValidator(string algClaim)
        {
            // We're expecting the format to be PX123
            if ((algClaim != null) && (algClaim.Length == 5))
            {
                for (int i = 0; i < this.validators.Length; i++)
                {
                    string prefix = this.validators[i].AlgorithmPrefix;
                    if ((algClaim[0] == prefix[0]) && (algClaim[1] == prefix[1]))
                    {
                        return this.validators[i];
                    }
                }
            }

            return null;
        }

        private bool GetValidator(JwtInformation jwt)
        {
            ISignatureValidator validator = this.FindValidator(jwt.Alg);
            if (validator == null)
            {
                Logger.InfoFormat("Unknown JWT signature algorithm: '{alg}'", jwt.Alg);
                return false;
            }

            HashAlgorithmName algorithmName = GetHashingAlgorithm(jwt.Alg);
            if (algorithmName.Name == null)
            {
                Logger.InfoFormat("Unsupported JWT hashing size: '{alg}'", jwt.Alg);
                return false;
            }

            jwt.AlgorithmName = algorithmName;
            jwt.Validator = validator;
            return true;
        }
    }
}
