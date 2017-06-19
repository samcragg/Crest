namespace OpenApi.UnitTests
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.OpenApi;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class OpenApiProviderTests
    {
        private readonly IOAdapter adapter = Substitute.For<IOAdapter>();
        private readonly OpenApiProvider provider;

        public OpenApiProviderTests()
        {
            this.adapter.OpenRead(null).ReturnsForAnyArgs(Stream.Null);
            this.adapter.OpenResource(null).ReturnsForAnyArgs(Stream.Null);
            this.provider = new OpenApiProvider(this.adapter);
        }

        public sealed class GetDirectRoutes : OpenApiProviderTests
        {
            [Theory]
            [InlineData("index.html")]
            [InlineData("swagger-ui.css", ".gz")]
            [InlineData("swagger-ui-bundle.js", ".gz")]
            [InlineData("swagger-ui-standalone-preset.js", ".gz")]
            public async Task ShouldIncludeEmbeddedResources(string file, string extension = "")
            {
                string expectedResource = "Crest.OpenApi.SwaggerUI." + file + extension;
                CheckResourceExists(expectedResource);

                DirectRouteMetadata route =
                    this.provider.GetDirectRoutes()
                        .Single(x => x.RouteUrl == "/doc/" + file);

                route.Verb.Should().Be("GET");
                IResponseData response = await route.Method(Substitute.For<IRequestData>(), Substitute.For<IContentConverter>());

                response.ContentType.Should().NotBeNullOrEmpty();
                await response.WriteBody(Stream.Null);
                this.adapter.Received().OpenResource(expectedResource);
            }

            [Fact]
            public async Task ShouldIncludeRedirectUrl()
            {
                DirectRouteMetadata redirect =
                    this.provider.GetDirectRoutes()
                        .Single(x => x.RouteUrl == "/doc");

                redirect.Verb.Should().Be("GET");
                IResponseData response = await redirect.Method(null, null);

                response.StatusCode.Should().Be(301);
                response.Headers["Location"].Should().Be("/doc/index.html");
            }

            [Fact]
            public async Task ShouldIncludeTheOpenApiJsonFile()
            {
                DirectRouteMetadata openApi =
                    this.provider.GetDirectRoutes()
                        .Single(x => x.RouteUrl == "/doc/openapi.json");

                openApi.Verb.Should().Be("GET");
                IResponseData response = await openApi.Method(null, null);

                await response.WriteBody(Stream.Null);
                this.adapter.Received().OpenRead("openapi.json");
            }

            [Fact]
            public async Task ShouldReturnIndexHtmlUncompressed()
            {
                IRequestData request = Substitute.For<IRequestData>();
                request.Headers["Accept-Encoding"].Returns("gzip");

                DirectRouteMetadata route =
                    this.provider.GetDirectRoutes()
                        .Single(x => x.RouteUrl == "/doc/index.html");

                IResponseData response = await route.Method(request, Substitute.For<IContentConverter>());

                response.Headers.Should().BeEmpty();
            }

            private void CheckResourceExists(string resourceName)
            {
                Assembly assebmly = typeof(OpenApiProvider).GetTypeInfo().Assembly;
                string[] resources = assebmly.GetManifestResourceNames();

                resources.Should().Contain(resourceName);
            }
        }
    }
}
