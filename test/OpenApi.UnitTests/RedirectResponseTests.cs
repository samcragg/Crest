namespace OpenApi.UnitTests
{
    using System.IO;
    using System.Threading.Tasks;
    using Crest.OpenApi;
    using FluentAssertions;
    using Xunit;

    public class RedirectResponseTests
    {
        private const string RedirectUri = "http://www.example.com/";
        private readonly RedirectResponse response = new RedirectResponse(RedirectUri);

        public sealed class ContentType : RedirectResponseTests
        {
            [Fact]
            public void ShouldBeEmpty()
            {
                this.response.ContentType.Should().BeEmpty();
            }
        }

        public sealed class Headers : RedirectResponseTests
        {
            [Fact]
            public void ShouldIncludeTheLocationHeader()
            {
                this.response.Headers.Should().ContainSingle();
                this.response.Headers["Location"].Should().Be(RedirectUri);
            }
        }

        public sealed class StatusCode : RedirectResponseTests
        {
            [Fact]
            public void ShouldReturnMovedPermanently()
            {
                this.response.StatusCode.Should().Be(301);
            }
        }

        public sealed class WriteBody : RedirectResponseTests
        {
            [Fact]
            public async Task ShouldNotWriteAnyContent()
            {
                using (var stream = new MemoryStream())
                {
                    await this.response.WriteBody(stream);
                    stream.Length.Should().Be(0);
                }
            }
        }
    }
}
