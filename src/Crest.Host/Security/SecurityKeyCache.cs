// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// Caches the values of security keys.
    /// </summary>
    internal partial class SecurityKeyCache
    {
        private readonly KeyProvider[] keyProviders;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityKeyCache"/> class.
        /// </summary>
        /// <param name="providers">
        /// The instances that provide the security tokens.
        /// </param>
        public SecurityKeyCache(ISecurityKeyProvider[] providers)
        {
            this.keyProviders = new KeyProvider[providers.Length];
            for (int i = 0; i < providers.Length; i++)
            {
                this.keyProviders[i] = new KeyProvider(providers[i]);
            }
        }

        /// <summary>
        /// Gets the known Elliptic Curve parameters.
        /// </summary>
        /// <returns>A sequence of valid Elliptic Curve parameters.</returns>
        public virtual IEnumerable<ECParameters> GetECParameters()
        {
            return this.keyProviders.SelectMany(p => p.EC ?? Enumerable.Empty<ECParameters>());
        }

        /// <summary>
        /// Gets the known RSA parameters.
        /// </summary>
        /// <returns>A sequence of valid RSA parameters.</returns>
        public virtual IEnumerable<RSAParameters> GetRsaParameters()
        {
            return this.keyProviders.SelectMany(p => p.Rsa ?? Enumerable.Empty<RSAParameters>());
        }

        /// <summary>
        /// Gets the known secret keys for symmetric algorithms.
        /// </summary>
        /// <returns>A sequence of valid secret key data.</returns>
        public virtual IEnumerable<byte[]> GetSecretKeys()
        {
            return this.keyProviders.SelectMany(p => p.SecretKeys ?? Enumerable.Empty<byte[]>());
        }

        /// <summary>
        /// Updates the cache to ensure all certificate information is valid.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous update operation.
        /// </returns>
        // TODO: This is not called :(
        public virtual Task UpdateCacheAsync()
        {
            unsafe
            {
                Span<int> indexes = stackalloc int[this.keyProviders.Length];
                int count = 0;
                for (int i = 0; i < this.keyProviders.Length; i++)
                {
                    if (this.keyProviders[i].Version != this.keyProviders[i].Provider.Version)
                    {
                        indexes[count++] = i;
                    }
                }

                if (count == 0)
                {
                    return Task.CompletedTask;
                }
                else
                {
                    return this.UpdateProvidersAsync(indexes.Slice(0, count));
                }
            }
        }

        private static IEnumerable<ECParameters> GetEcParameters(X509Certificate2[] certificates)
        {
            for (int i = 0; i < certificates.Length; i++)
            {
                ECDsa key = certificates[i].GetECDsaPublicKey();
                if (key != null)
                {
                    yield return key.ExportParameters(includePrivateParameters: false);
                    key.Dispose();
                }
            }
        }

        private static IEnumerable<RSAParameters> GetRsaParameters(X509Certificate2[] certificates)
        {
            for (int i = 0; i < certificates.Length; i++)
            {
                RSA key = certificates[i].GetRSAPublicKey();
                if (key != null)
                {
                    yield return key.ExportParameters(includePrivateParameters: false);
                    key.Dispose();
                }
            }
        }

        private static async Task UpdateProvider(KeyProvider provider)
        {
            Task<X509Certificate2[]> certificatesTask = provider.Provider.GetCertificatesAsync();
            Task<byte[][]> keysTask = provider.Provider.GetSecretKeysAsync();
            await Task.WhenAll(certificatesTask, keysTask).ConfigureAwait(false);

            X509Certificate2[] certificates = await certificatesTask.ConfigureAwait(false);
            byte[][] keys = await keysTask.ConfigureAwait(false);

            // Allow the caller to assign to the provider.UpdateTask property
            // BEFORE we clear it here (this will happen if the provider
            // methods complete on the same thread, for example, by using
            // Task.FromResult)
            await Task.Yield();

            lock (provider)
            {
                provider.Version = provider.Provider.Version;
                provider.UpdateTask = null;
                provider.SecretKeys = keys;
                provider.EC = GetEcParameters(certificates).ToArray();
                provider.Rsa = GetRsaParameters(certificates).ToArray();

                foreach (X509Certificate2 certificate in certificates)
                {
                    certificate.Dispose();
                }
            }
        }

        private Task UpdateProvidersAsync(Span<int> indexes)
        {
            var tasks = new List<Task>(indexes.Length);
            for (int i = 0; i < indexes.Length; i++)
            {
                KeyProvider provider = this.keyProviders[indexes[i]];
                lock (provider)
                {
                    Task updateTask = provider.UpdateTask;
                    if (updateTask == null)
                    {
                        updateTask = UpdateProvider(provider);
                        provider.UpdateTask = updateTask;
                    }

                    tasks.Add(updateTask);
                }
            }

            return Task.WhenAll(tasks);
        }
    }
}
