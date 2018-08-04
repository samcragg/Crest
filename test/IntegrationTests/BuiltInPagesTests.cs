namespace IntegrationTests
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using HtmlAgilityPack;
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
            using (HttpClient client = this.fixture.CreateClient())
            {
                HttpResponseMessage response = await client.GetAsync("/docs");

                response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
                response.Headers.Location.ToString()
                    .Should().EndWithEquivalent("docs/index.html");
            }
        }

        [Fact]
        public async Task EnsureDocsReturnsTheHtmlViewer()
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(await this.GetResultAsync("/docs/index.html"));

            htmlDoc.DocumentNode.SelectSingleNode("//body")
                .Should().NotBeNull();
        }

        [Fact]
        public async Task EnsureDocsReturnTheOpenApiData()
        {
            string result = await this.GetResultAsync("/docs/V1/OpenAPI.json");

            result.Should().Be("Documentation information");
        }

        [Fact]
        public async Task EnsureHealthPageIsPresent()
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(await this.GetResultAsync("/health"));

            htmlDoc.DocumentNode.SelectSingleNode("//h1").InnerText
                .Should().Be("Service Health");
        }

        [Fact]
        public async Task EnsureMetricsJsonIsPresent()
        {
            string result = await this.GetResultAsync("/metrics.json");

            var json = JToken.Parse(result);
            json.Should().Contain(j => j.Path == "requestCount");
        }

        private async Task<string> GetResultAsync(string url)
        {
            // There's an issue with the TestServer where it can end the request
            // if it takes too long. For now, brute force it by retrying...
            for (int i = 0; i < 16; i++)
            {
                HttpClient client = this.fixture.CreateClient();
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.IsSuccessStatusCode.Should().BeTrue();

                    return await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException)
                {
                }
                finally
                {
                    client.Dispose();
                }
            }

            throw new Xunit.Sdk.XunitException("Unable to read the response.");
        }
    }
}
