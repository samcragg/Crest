namespace IntegrationTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Integration")]
    public sealed class SerializationTests : IClassFixture<WebFixture>
    {
        private readonly WebFixture fixture;

        public SerializationTests(WebFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async Task EnsureLinkCollectionsAreSerialized()
        {
            using (HttpClient client = this.fixture.CreateAuthenticatedClient())
            {
                var message = new HttpRequestMessage(HttpMethod.Get, "/v1/links/www.example.com");

                dynamic result = await this.fixture.GetResultAsync(client, message);

                ((string)result.linkName.hRef).Should().Be("http://www.example.com/");
            }
        }
    }
}
