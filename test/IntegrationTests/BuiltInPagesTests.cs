namespace IntegrationTests
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using HtmlAgilityPack;
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
    }
}
