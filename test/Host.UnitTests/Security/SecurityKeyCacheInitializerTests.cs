namespace Host.UnitTests.Security
{
    using System;
    using System.Threading.Tasks;
    using Crest.Host.Security;
    using FluentAssertions;
    using NSubstitute;
    using NSubstitute.ExceptionExtensions;
    using Xunit;

    public class SecurityKeyCacheInitializerTests : IDisposable
    {
        private const int UpdateMs = 64;
        private readonly SecurityKeyCache cache = Substitute.For<SecurityKeyCache>();
        private readonly SecurityKeyCacheInitializer initializer;

        private SecurityKeyCacheInitializerTests()
        {
            SecurityKeyCacheInitializer.UpdateFrequency = TimeSpan.FromMilliseconds(UpdateMs);
            this.initializer = new SecurityKeyCacheInitializer(this.cache);
        }

        void IDisposable.Dispose()
        {
            this.initializer.Dispose();
        }

        public sealed class Dispose : SecurityKeyCacheInitializerTests
        {
            [Fact]
            public async Task ShouldStopTheUpdatingOfTheCache()
            {
                await this.initializer.InitializeAsync(null, null);
                this.cache.ClearReceivedCalls();

                this.initializer.Dispose();
                await Task.Delay(UpdateMs + 32);

                await this.cache.DidNotReceive().UpdateCacheAsync();
            }
        }

        public sealed class InitializeAsync : SecurityKeyCacheInitializerTests
        {
            [Fact]
            public void ShouldHandleErrorsFromUpdatingTheCache()
            {
                this.cache.UpdateCacheAsync().Throws<DivideByZeroException>();

                this.initializer.Awaiting(x => x.InitializeAsync(null, null))
                    .Should().NotThrow();
            }

            [Fact]
            public async Task ShouldUpdateTheCache()
            {
                await this.initializer.InitializeAsync(null, null);

                await this.cache.Received().UpdateCacheAsync();
            }

            [Fact]
            public async Task ShouldUpdateTheCacheAtRegularIntervals()
            {
                await this.initializer.InitializeAsync(null, null);
                this.cache.ClearReceivedCalls();

                await Task.Delay(UpdateMs + 32);
                await this.cache.Received().UpdateCacheAsync();
                this.cache.ClearReceivedCalls();

                await Task.Delay(UpdateMs + 32);
                await this.cache.Received().UpdateCacheAsync();
            }
        }
    }
}
