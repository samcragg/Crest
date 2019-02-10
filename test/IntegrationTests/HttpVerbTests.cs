namespace IntegrationTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using IntegrationTests.Services;
    using Xunit;

    [Trait("Category", "Integration")]
    public sealed class HttpVerbTests : IClassFixture<WebFixture>
    {
        private readonly WebFixture fixture;

        public HttpVerbTests(WebFixture fixture)
        {
            this.fixture = fixture;
        }

        [Theory]
        [InlineData("DELETE")]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("PUT")]
        public async Task EnsureRouteWorksForVerb(string verb)
        {
            using (HttpClient client = this.fixture.CreateAuthenticatedClient())
            {
                var message = new HttpRequestMessage(
                    new HttpMethod(verb),
                    "/v1" + HttpVerbs.Endpoint);

                string content = await this.fixture.GetResultAsStringAsync(client, message);

                content.Should().BeEquivalentTo(verb);
            }
        }
    }
}
