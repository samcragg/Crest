namespace Host.UnitTests.Routing
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.Diagnostics;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class HealthPageProviderTests
    {
        private readonly HealthPage page = Substitute.For<HealthPage>();
        private readonly HealthPageProvider provider;

        public HealthPageProviderTests()
        {
            this.provider = new HealthPageProvider(this.page);
        }

        public sealed class GetDirectRoutes : HealthPageProviderTests
        {
            [Fact]
            public void ShouldReturnTheHealthPageInformation()
            {
                IEnumerable<DirectRouteMetadata> metadata = this.provider.GetDirectRoutes();

                metadata.Should().ContainSingle(m => m.RouteUrl == "/health")
                        .Which.Verb.Should().Be("GET");
            }

            [Fact]
            public async Task ShouldWriteTheHealthPageInformation()
            {
                IRequestData request = Substitute.For<IRequestData>();
                IContentConverter converter = Substitute.For<IContentConverter>();
                Stream stream = Substitute.For<Stream>();

                DirectRouteMetadata metadata =
                    this.provider.GetDirectRoutes().Single(d => d.RouteUrl == "/health");

                IResponseData response = await metadata.Method(request, converter);
                await response.WriteBody(stream);

                await this.page.Received().WriteToAsync(stream);
            }
        }
    }
}
