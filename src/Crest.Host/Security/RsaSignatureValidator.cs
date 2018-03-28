// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System.Security.Cryptography;

    /// <summary>
    /// Allows the validation of RSA signatures.
    /// </summary>
    internal sealed class RsaSignatureValidator : AsymmetricSignatureValidator
    {
        private readonly SecurityKeyCache keys;

        /// <summary>
        /// Initializes a new instance of the <see cref="RsaSignatureValidator"/> class.
        /// </summary>
        /// <param name="keys">The cache containing the security keys.</param>
        public RsaSignatureValidator(SecurityKeyCache keys)
        {
            this.keys = keys;
        }

        /// <inheritdoc />
        public override string AlgorithmPrefix => "RS";

        /// <inheritdoc />
        protected override bool ValidateHash(byte[] hash, byte[] signature, HashAlgorithmName algorithm)
        {
            using (var rsa = RSA.Create())
            {
                foreach (RSAParameters parameters in this.keys.GetRsaParameters())
                {
                    rsa.ImportParameters(parameters);
                    if (rsa.VerifyHash(hash, signature, algorithm, RSASignaturePadding.Pkcs1))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
