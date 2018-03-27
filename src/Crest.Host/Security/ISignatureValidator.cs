// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System.Security.Cryptography;

    /// <summary>
    /// Determines whether a signature is valid or not.
    /// </summary>
    internal interface ISignatureValidator
    {
        /// <summary>
        /// Gets the prefix of the hashing algorithm.
        /// </summary>
        string AlgorithmPrefix { get; }

        /// <summary>
        /// Determines whether the specified signature is valid.
        /// </summary>
        /// <param name="data">
        /// The base64 encoded header and payload data.
        /// </param>
        /// <param name="signature">The decoded signature.</param>
        /// <param name="algorithm">The name of the hashing algorithm.</param>
        /// <returns>
        /// <c>true</c> if the signature is valid; otherwise, <c>false</c>.
        /// </returns>
        bool IsValid(byte[] data, byte[] signature, HashAlgorithmName algorithm);
    }
}
