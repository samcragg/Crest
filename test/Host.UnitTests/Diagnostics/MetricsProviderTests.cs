namespace Host.UnitTests.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host;
    using Crest.Host.Diagnostics;
    using Crest.Host.Engine;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class MetricsProviderTests
    {
        private readonly HostingEnvironment environment;
        private readonly HostingOptions options;
        private readonly Metrics metrics;
        private readonly Lazy<MetricsProvider> provider;

        private MetricsProviderTests()
        {
            this.metrics = Substitute.For<Metrics>();
            this.options = new HostingOptions();
            this.environment = Substitute.For<HostingEnvironment>();

            this.provider = new Lazy<MetricsProvider>(
                () => new MetricsProvider(this.metrics, this.environment, this.options));
        }

        private MetricsProvider Provider => this.provider.Value;

        public sealed class GetDirectRoutes : MetricsProviderTests
        {
            [Fact]
            public void ShouldBeEnabledIfConfigurationIsTrue()
            {
                this.environment.IsDevelopment.Returns(false);
                this.options.DisplayMetrics = true;

                IEnumerable<DirectRouteMetadata> result = this.Provider.GetDirectRoutes();

                result.Should().NotBeEmpty();
            }

            [Fact]
            public void ShouldBeEnabledInDevelopmentEnvironments()
            {
                this.environment.IsDevelopment.Returns(true);

                IEnumerable<DirectRouteMetadata> result = this.Provider.GetDirectRoutes();

                result.Should().NotBeEmpty();
            }

            [Fact]
            public void ShouldNotBeAnabledIfNotInDevelopmentMode()
            {
                this.environment.IsDevelopment.Returns(false);

                IEnumerable<DirectRouteMetadata> result = this.Provider.GetDirectRoutes();

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldNotBeEnabledIfConfigurationIsFalse()
            {
                this.environment.IsDevelopment.Returns(true);
                this.options.DisplayMetrics = false;

                IEnumerable<DirectRouteMetadata> result = this.Provider.GetDirectRoutes();

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnTheJsonFile()
            {
                this.options.DisplayMetrics = true;

                IEnumerable<DirectRouteMetadata> metadata = this.Provider.GetDirectRoutes();

                metadata.Should().ContainSingle(m => m.RouteUrl == "/metrics.json")
                        .Which.Verb.Should().Be("GET");
            }

            [Fact]
            public async Task ShouldWriteTheMetricInformation()
            {
                this.options.DisplayMetrics = true;
                IRequestData request = Substitute.For<IRequestData>();
                Stream stream = Substitute.For<Stream>();

                DirectRouteMetadata metadata =
                    this.Provider.GetDirectRoutes().Single(d => d.RouteUrl == "/metrics.json");

                IResponseData response = await metadata.Method(request, null);
                await response.WriteBody(stream);

                this.metrics.ReceivedWithAnyArgs().WriteTo(null);
            }
        }
    }
}
