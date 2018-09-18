// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Core.Logging;

    /// <summary>
    /// Initializes the <see cref="SecurityKeyCache"/> and keeps it updated.
    /// </summary>
    internal sealed class SecurityKeyCacheInitializer : IStartupInitializer, IDisposable
    {
        private static readonly ILog Logger = Log.For<SecurityKeyCacheInitializer>();
        private readonly SecurityKeyCache cache;
        private readonly CancellationTokenSource cancellationToken = new CancellationTokenSource();

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityKeyCacheInitializer"/> class.
        /// </summary>
        /// <param name="cache">The cache to initialize.</param>
        public SecurityKeyCacheInitializer(SecurityKeyCache cache)
        {
            this.cache = cache;
        }

        /// <summary>
        /// Gets or sets the amount of time to wait before updating the cache.
        /// </summary>
        /// <remarks>
        /// This is exposed for unit testing.
        /// </remarks>
        internal static TimeSpan UpdateFrequency { get; set; }
            = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Stops the updating of the cache.
        /// </summary>
        public void Dispose()
        {
            if (!this.cancellationToken.IsCancellationRequested)
            {
                this.cancellationToken.Cancel();
                this.cancellationToken.Dispose();
            }
        }

        /// <inheritdoc />
        public async Task InitializeAsync(IServiceRegister serviceRegister, IServiceLocator serviceLocator)
        {
            // Make sure it's up to date during startup
            await this.UpdateCacheAsync().ConfigureAwait(false);

            // Now make sure it gets updated regularly. DO NOT wait for this,
            // as it will continue forever
            _ = this.UpdateLoopAsync(this.cancellationToken.Token);
        }

        private async Task UpdateCacheAsync()
        {
            Logger.Info("Updating security key cache");
            try
            {
                await this.cache.UpdateCacheAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error updating the security key cache", ex);
            }
        }

        private async Task UpdateLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(UpdateFrequency, cancellationToken).ConfigureAwait(false);
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.UpdateCacheAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
