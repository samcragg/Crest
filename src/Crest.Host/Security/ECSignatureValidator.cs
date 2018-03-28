// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System.Security.Cryptography;

    /// <summary>
    /// Allows the validation of Elliptic Curve signatures.
    /// </summary>
    internal sealed class ECSignatureValidator : AsymmetricSignatureValidator
    {
        private readonly SecurityKeyCache keys;

        /// <summary>
        /// Initializes a new instance of the <see cref="ECSignatureValidator"/> class.
        /// </summary>
        /// <param name="keys">The cache containing the security keys.</param>
        public ECSignatureValidator(SecurityKeyCache keys)
        {
            this.keys = keys;
        }

        /// <inheritdoc />
        public override string AlgorithmPrefix => "ES";

        /// <inheritdoc />
        protected override bool ValidateHash(byte[] hash, byte[] signature, HashAlgorithmName algorithm)
        {
            using (var ec = ECDsa.Create())
            {
                foreach (ECParameters parameters in this.keys.GetECParameters())
                {
                    ec.ImportParameters(parameters);
                    if (ec.VerifyHash(hash, signature))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
