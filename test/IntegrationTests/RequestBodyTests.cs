namespace IntegrationTests
{
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Xunit;

    [Trait("Category", "Integration")]
    public sealed class RequestBodyTests : IClassFixture<WebFixture>
    {
        private readonly WebFixture fixture;

        public RequestBodyTests(WebFixture fixture)
        {
            this.fixture = fixture;
        }

        [Theory]
        [InlineData("application/json", "\"string\"")]
        [InlineData("application/xml", "<String>string</String>")]
        public async Task CanAcceptDifferentContentTypes(string mediaType, string content)
        {
            using (HttpClient client = this.fixture.CreateAuthenticatedClient())
            {
                var message = new HttpRequestMessage(HttpMethod.Put, "/v1/string");
                message.Content = new StringContent(content, Encoding.UTF8, mediaType);

                string result = await this.fixture.GetResultAsStringAsync(client, message);

                result.Should().BeEquivalentTo("string");
            }
        }

        [Fact]
        public async Task CanHandleFiles()
        {
            using (HttpClient client = this.fixture.CreateAuthenticatedClient())
            {
                const string NewLine = "\r\n";
                const string MultipartMessage = "--boundary" + NewLine +
"Content-Disposition: inline; filename=filename.txt" + NewLine +
NewLine +
"File contents" + NewLine +
"--boundary--";

                var message = new HttpRequestMessage(HttpMethod.Post, "/v1/file")
                {
                    Content = new StringContent(MultipartMessage, Encoding.UTF8)
                };
                message.Content.Headers.Remove("Content-Type");
                message.Content.Headers.TryAddWithoutValidation("Content-Type", "multipart/form-data; boundary=boundary");

                string result = await this.fixture.GetResultAsStringAsync(client, message);

                result.Should().BeEquivalentTo("filename.txt");
            }
        }
    }
}
