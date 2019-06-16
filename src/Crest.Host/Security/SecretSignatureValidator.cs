// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System.Security.Cryptography;

    /// <summary>
    /// Allows the validation of shared secret signatures.
    /// </summary>
    internal sealed class SecretSignatureValidator : ISignatureValidator
    {
        private readonly SecurityKeyCache keys;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretSignatureValidator"/> class.
        /// </summary>
        /// <param name="keys">The cache containing the security keys.</param>
        public SecretSignatureValidator(SecurityKeyCache keys)
        {
            this.keys = keys;
        }

        /// <inheritdoc />
        public string AlgorithmPrefix => "HS";

        /// <inheritdoc />
        public bool IsValid(byte[] data, byte[] signature, HashAlgorithmName algorithm)
        {
            if (signature != null)
            {
                using (HMAC hmac = GetAlgorithm(algorithm))
                {
                    if ((hmac == null) || (signature.Length != (hmac.HashSize / 8)))
                    {
                        return false;
                    }

                    foreach (byte[] secret in this.keys.GetSecretKeys())
                    {
                        hmac.Key = secret;
                        byte[] hash = hmac.ComputeHash(data);
                        if (ArraysAreEqual(signature, hash))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool ArraysAreEqual(byte[] a, byte[] b)
        {
            // We've checked for null and length in the caller
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static HMAC GetAlgorithm(HashAlgorithmName name)
        {
            switch (name.Name)
            {
                case "SHA256":
                    return new HMACSHA256();

                case "SHA384":
                    return new HMACSHA384();

                case "SHA512":
                    return new HMACSHA512();

                default:
                    return null;
            }
        }
    }
}
