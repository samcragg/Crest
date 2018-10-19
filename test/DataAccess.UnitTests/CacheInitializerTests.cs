namespace DataAccess.UnitTests
{
    using System;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.DataAccess;
    using Crest.DataAccess.Expressions;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class CacheInitializerTests
    {
        private readonly CacheInitializer initializer = new CacheInitializer();

        public sealed class InitializeAsync : CacheInitializerTests, IDisposable
        {
            public void Dispose()
            {
                QueryableExtensions.MappingCache = null;
            }

            [Fact]
            public async Task ShouldSetTheStaticMappingCache()
            {
                MappingCache cache = Substitute.For<MappingCache>();
                IServiceLocator locator = Substitute.For<IServiceLocator>();
                locator.GetService(typeof(MappingCache)).Returns(cache);

                await this.initializer.InitializeAsync(null, locator);

                QueryableExtensions.MappingCache.Should().BeSameAs(cache);
            }
        }
    }
}
