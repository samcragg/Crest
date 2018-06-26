namespace IntegrationTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Integration")]
    public sealed class VersionTests : IClassFixture<WebFixture>
    {
        private readonly WebFixture fixture;

        public VersionTests(WebFixture fixture)
        {
            this.fixture = fixture;
        }

        [Theory]
        [InlineData("/v1/version", "version 1")]
        [InlineData("/v2/version", "version 2")]
        public async Task EnsureRouteWorksForVerb(string route, string expected)
        {
            using (HttpClient client = this.fixture.CreateAuthenticatedClient())
            {
                var message = new HttpRequestMessage(HttpMethod.Get, route);

                string result = await this.fixture.GetResultAsStringAsync(client, message);

                result.Should().BeEquivalentTo(expected);
            }
        }
    }
}
