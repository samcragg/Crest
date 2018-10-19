namespace IntegrationTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Integration")]
    public sealed class QueryTests : IClassFixture<WebFixture>
    {
        private readonly WebFixture fixture;

        public QueryTests(WebFixture fixture)
        {
            this.fixture = fixture;
        }

        [Theory]
        [InlineData("/v1/any?stringValue=string&intValue=123", "string 123")]
        [InlineData("/v1/both?stringValue=string&intValue=123&value=test", "test string 123")]
        [InlineData("/v1/captured?value=test", "test")]
        public async Task EnsureQueryParametersAreCaptured(string route, string expected)
        {
            using (HttpClient client = this.fixture.CreateAuthenticatedClient())
            {
                var message = new HttpRequestMessage(HttpMethod.Get, route);

                string result = await this.fixture.GetResultAsStringAsync(client, message);

                result.Should().BeEquivalentTo(expected);
            }
        }

        [Theory]
        [InlineData("value=lt:4", "1,2,3")]
        [InlineData("order=desc:value", "5,4,3,2,1")]
        [InlineData("value=gt:3&order=desc:value", "5,4")]
        public async Task EnsureFilterAndSorting(string query, string expected)
        {
            using (HttpClient client = this.fixture.CreateAuthenticatedClient())
            {
                var message = new HttpRequestMessage(HttpMethod.Get, "/v1/queryable?" + query);

                string result = await this.fixture.GetResultAsStringAsync(client, message);

                result.Should().BeEquivalentTo(expected);
            }
        }
    }
}
