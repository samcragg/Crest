// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the provision of security keys that can be used for validating
    /// the signatures of tokens.
    /// </summary>
    public interface ISecurityKeyProvider
    {
        /// <summary>
        /// Gets the current version of the data.
        /// </summary>
        /// <remarks>
        /// This is used to allow caching of values - when the cache should be
        /// invalidated the property should return a different value.
        /// </remarks>
        int Version { get; }

        /// <summary>
        /// Gets a list of valid certificates that can be used to verify
        /// signatures.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous fetch operation. The value
        /// of the Result property contains an array of validated certificates.
        /// </returns>
        Task<X509Certificate2[]> GetCertificatesAsync();

        /// <summary>
        /// Gets a list of valid symmetrical secret keys that can be used to
        /// verify signatures.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous fetch operation. The value
        /// of the Result property contains an array of secret key byte arrays.
        /// </returns>
        Task<byte[][]> GetSecretKeysAsync();
    }
}
