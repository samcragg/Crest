// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System.Security.Cryptography;

    /// <summary>
    /// Allows the verification of asymmetric key signed data.
    /// </summary>
    internal abstract class AsymmetricSignatureValidator : ISignatureValidator
    {
        /// <inheritdoc />
        public abstract string AlgorithmPrefix { get; }

        /// <inheritdoc />
        public bool IsValid(byte[] data, byte[] signature, HashAlgorithmName algorithm)
        {
            HashAlgorithm hashAlgorithm = CreateHash(algorithm);
            if (hashAlgorithm == null)
            {
                return false;
            }

            using (hashAlgorithm)
            {
                byte[] hash = hashAlgorithm.ComputeHash(data);
                return this.ValidateHash(hash, signature, algorithm);
            }
        }

        /// <summary>
        /// Determines whether the specified signature is valid.
        /// </summary>
        /// <param name="hash">
        /// The hash of the bytes from base64 encoded header and payload.
        /// </param>
        /// <param name="signature">The decoded signature.</param>
        /// <param name="algorithm">The name of the hashing algorithm.</param>
        /// <returns>
        /// <c>true</c> if the signature is valid; otherwise, <c>false</c>.
        /// </returns>
        protected abstract bool ValidateHash(byte[] hash, byte[] signature, HashAlgorithmName algorithm);

        private static HashAlgorithm CreateHash(HashAlgorithmName name)
        {
            switch (name.Name)
            {
                case "SHA256":
                    return SHA256.Create();

                case "SHA384":
                    return SHA384.Create();

                case "SHA512":
                    return SHA512.Create();

                default:
                    return null;
            }
        }
    }
}
