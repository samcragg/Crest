namespace Host.UnitTests.Diagnostics
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class MetricsProviderTests
    {
        private readonly Metrics metrics;
        private readonly MetricsProvider provider;

        private MetricsProviderTests()
        {
            this.metrics = Substitute.For<Metrics>();
            this.provider = new MetricsProvider(this.metrics);
        }

        public sealed class GetDirectRoutes : MetricsProviderTests
        {
            [Fact]
            public void ShouldReturnTheJsonFile()
            {
                IEnumerable<DirectRouteMetadata> metadata = this.provider.GetDirectRoutes();

                metadata.Should().ContainSingle(m => m.RouteUrl == "/metrics.json")
                        .Which.Verb.Should().Be("GET");
            }

            [Fact]
            public async Task ShouldWriteTheMetricInformation()
            {
                IRequestData request = Substitute.For<IRequestData>();
                Stream stream = Substitute.For<Stream>();

                DirectRouteMetadata metadata =
                    this.provider.GetDirectRoutes().Single(d => d.RouteUrl == "/metrics.json");

                IResponseData response = await metadata.Method(request, null);
                await response.WriteBody(stream);

                this.metrics.ReceivedWithAnyArgs().WriteTo(null);
            }
        }
    }
}
