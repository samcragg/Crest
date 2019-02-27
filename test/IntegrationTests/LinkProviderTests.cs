namespace IntegrationTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Integration")]
    public sealed class LinkProviderTests : IClassFixture<WebFixture>
    {
        private readonly WebFixture fixture;

        public LinkProviderTests(WebFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task EnsureLinkProviderResolvesTheCorrectValue()
        {
            using (HttpClient client = this.fixture.CreateAuthenticatedClient())
            {
                var message = new HttpRequestMessage(HttpMethod.Get, "/v1/links/to/test");

                dynamic result = await this.fixture.GetResultAsync(client, message);

                ((string)result.hRef).Should().Be("/v1/links/test");
            }
        }
    }
}

