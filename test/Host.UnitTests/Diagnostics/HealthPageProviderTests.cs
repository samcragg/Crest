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

    public class HealthPageProviderTests
    {
        private readonly HostingEnvironment environment;
        private readonly HostingOptions options;
        private readonly HealthPage page;
        private readonly Lazy<HealthPageProvider> provider;

        private HealthPageProviderTests()
        {
            this.options = new HostingOptions();
            this.page = Substitute.For<HealthPage>();
            this.environment = Substitute.For<HostingEnvironment>();

            this.provider = new Lazy<HealthPageProvider>(
                () => new HealthPageProvider(this.page, this.environment, this.options));
        }

        private HealthPageProvider Provider => this.provider.Value;

        public sealed class GetDirectRoutes : HealthPageProviderTests
        {
            [Fact]
            public void ShouldBeEnabledIfConfigurationIsTrue()
            {
                this.environment.IsDevelopment.Returns(false);
                this.options.DisplayHealth = true;

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
                this.options.DisplayHealth = false;

                IEnumerable<DirectRouteMetadata> result = this.Provider.GetDirectRoutes();

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnTheHealthPageInformation()
            {
                this.options.DisplayHealth = true;

                IEnumerable<DirectRouteMetadata> metadata = this.Provider.GetDirectRoutes();

                metadata.Should().ContainSingle(m => m.RouteUrl == "/health")
                        .Which.Verb.Should().Be("GET");
            }

            [Fact]
            public async Task ShouldWriteTheHealthPageInformation()
            {
                this.options.DisplayHealth = true;
                IRequestData request = Substitute.For<IRequestData>();
                IContentConverter converter = Substitute.For<IContentConverter>();
                Stream stream = Substitute.For<Stream>();

                DirectRouteMetadata metadata =
                    this.Provider.GetDirectRoutes().Single(d => d.RouteUrl == "/health");

                IResponseData response = await metadata.Method(request, converter);
                await response.WriteBody(stream);

                await this.page.Received().WriteToAsync(stream);
            }
        }
    }
}
