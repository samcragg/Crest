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
        private readonly Timer timer;
        private readonly object timerLock = new object();
        private volatile bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityKeyCacheInitializer"/> class.
        /// </summary>
        /// <param name="cache">The cache to initialize.</param>
        public SecurityKeyCacheInitializer(SecurityKeyCache cache)
        {
            this.cache = cache;
            this.timer = new Timer(_ => Task.Run(this.UpdateCacheAsync));
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
            if (!this.disposed)
            {
                lock (this.timerLock)
                {
                    this.disposed = true;
                    this.timer.Dispose();
                }
            }
        }

        /// <inheritdoc />
        public async Task InitializeAsync(IServiceRegister serviceRegister, IServiceLocator serviceLocator)
        {
            // Make sure it's up to date during startup
            await this.UpdateCacheAsync().ConfigureAwait(false);
            this.ScheduleCallback();
        }

        private void ScheduleCallback()
        {
            lock (this.timerLock)
            {
                if (!this.disposed)
                {
                    int delayMs = (int)UpdateFrequency.TotalMilliseconds;
                    Logger.Info("Scheduling an update of the security key cache in {0}ms", delayMs);
                    this.timer.Change(delayMs, -1);
                }
            }
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

            this.ScheduleCallback();
        }
    }
}
