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
        private readonly IndexHtmlGenerator generator = Substitute.For<IndexHtmlGenerator>();
        private readonly IOAdapter io = Substitute.For<IOAdapter>();
        private readonly OpenApiProvider provider;
        private readonly SpecificationFileLocator specFiles = Substitute.For<SpecificationFileLocator>();

        public OpenApiProviderTests()
        {
            this.io.OpenRead(null).ReturnsForAnyArgs(Stream.Null);
            this.io.OpenResource(null).ReturnsForAnyArgs(Stream.Null);
            this.provider = new OpenApiProvider(this.io, this.specFiles, this.generator);
        }

        public sealed class GetDirectRoutes : OpenApiProviderTests
        {
            private const string UrlBase = OpenApiProvider.DocumentationBaseRoute;

            [Theory]
            [InlineData("swagger-ui.css", ".gz")]
            [InlineData("swagger-ui-bundle.js", ".gz")]
            [InlineData("swagger-ui-standalone-preset.js", ".gz")]
            public async Task ShouldIncludeEmbeddedResources(string file, string extension)
            {
                string expectedResource = "Crest.OpenApi.SwaggerUI." + file + extension;
                CheckResourceExists(expectedResource);

                DirectRouteMetadata route =
                    this.provider.GetDirectRoutes()
                        .Single(x => x.RouteUrl == (UrlBase + "/" + file));

                route.Verb.Should().Be("GET");
                IResponseData response = await route.Method(Substitute.For<IRequestData>(), Substitute.For<IContentConverter>());

                response.ContentType.Should().NotBeNullOrEmpty();
                await response.WriteBody(Stream.Null);
                this.io.Received().OpenResource(expectedResource);
            }

            [Fact]
            public async Task ShouldIncludeTheOpenApiJsonFile()
            {
                this.specFiles.RelativePaths.Returns(new[] { "v1/openapi.json.gz" });

                DirectRouteMetadata openApi =
                    this.provider.GetDirectRoutes()
                        .Single(x => x.RouteUrl == UrlBase + "/v1/openapi.json");

                openApi.Verb.Should().Be("GET");
                IResponseData response = await openApi.Method(
                    Substitute.For<IRequestData>(),
                    Substitute.For<IContentConverter>());

                await response.WriteBody(Stream.Null);
                this.io.Received().OpenRead(Arg.Is<string>(s => s.EndsWith("openapi.json.gz")));
            }

            [Fact]
            public async Task ShouldRedirectToIndexHtml()
            {
                DirectRouteMetadata redirect =
                    this.provider.GetDirectRoutes()
                        .Single(x => x.RouteUrl == UrlBase);

                redirect.Verb.Should().Be("GET");
                IResponseData response = await redirect.Method(null, null);

                response.StatusCode.Should().Be(301);
                response.Headers["Location"].Should().Be(UrlBase + "/index.html");
            }

            [Fact]
            public async Task ShouldReturnIndexHtmlUncompressed()
            {
                IRequestData request = Substitute.For<IRequestData>();
                request.Headers["Accept-Encoding"].Returns("gzip");

                DirectRouteMetadata route =
                    this.provider.GetDirectRoutes()
                        .Single(x => x.RouteUrl == UrlBase + "/index.html");

                IResponseData response = await route.Method(request, Substitute.For<IContentConverter>());

                response.Headers.Should().BeEmpty();
            }

            [Fact]
            public async Task ShouldTransformTheIndexHtml()
            {
                this.generator.GetPage().Returns(Stream.Null);

                DirectRouteMetadata route =
                    this.provider.GetDirectRoutes()
                        .Single(x => x.RouteUrl == (UrlBase + "/index.html"));

                IResponseData response = await route.Method(Substitute.For<IRequestData>(), Substitute.For<IContentConverter>());

                await response.WriteBody(Stream.Null);
                this.generator.Received().GetPage();
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
