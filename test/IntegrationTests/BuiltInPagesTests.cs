namespace IntegrationTests
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using HtmlAgilityPack;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json.Linq;
    using Xunit;

    [Trait("Category", "Integration")]
    public sealed class BuiltInPagesTests : IClassFixture<WebFixture>
    {
        private readonly WebFixture fixture;

        public BuiltInPagesTests(WebFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task EnsureDocsRedirectsToTheIndex()
        {
            HttpResponse response = await this.fixture.GetRawResponse("/docs");

            response.StatusCode.Should().Be((int)HttpStatusCode.MovedPermanently);
            response.Headers["Location"].ToString()
                .Should().EndWithEquivalent("docs/index.html");
        }

        [Fact]
        public async Task EnsureDocsReturnsTheHtmlViewer()
        {
            using (HttpClient client = this.fixture.CreateClient())
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(await client.GetStringAsync("/docs/index.html"));

                htmlDoc.DocumentNode.SelectSingleNode("//body")
                    .Should().NotBeNull();
            }
        }

        [Fact]
        public async Task EnsureDocsReturnTheOpenApiData()
        {
            using (HttpClient client = this.fixture.CreateClient())
            {
                string result = await client.GetStringAsync("/docs/V1/OpenAPI.json");

                result.Should().Be("Documentation information");
            }
        }

        [Fact]
        public async Task EnsureHealthPageIsPresent()
        {
            // Because the health page takes a bit longer to load, the TestServer
            // disposes of the stream before we've written everything to it, so
            // we're just checking the response was found
            HttpResponse response = await this.fixture.GetRawResponse("/health");

            response.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        [Fact]
        public async Task EnsureMetricsJsonIsPresent()
        {
            using (HttpClient client = this.fixture.CreateClient())
            {
                string result = await client.GetStringAsync("/metrics.json");

                var json = JToken.Parse(result);
                json.Should().Contain(j => j.Path == "requestCount");
            }
        }
    }
}
