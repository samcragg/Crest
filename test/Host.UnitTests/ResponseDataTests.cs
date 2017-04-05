namespace Host.UnitTests
{
    using System.IO;
    using System.Threading.Tasks;
    using Crest.Host;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class ResponseDataTests
    {
        public sealed class WriteBody : ResponseDataTests
        {
            [Fact]
            public void ShouldNotBeNull()
            {
                var data = new ResponseData("", 0, body: null);

                data.WriteBody.Should().NotBeNull();
            }

            [Fact]
            public void ShouldReturnACompletedTask()
            {
                var data = new ResponseData("", 0, body: null);
                var stream = Substitute.For<Stream>();

                Task write = data.WriteBody(stream);

                write.Should().NotBeNull();
                write.IsCompleted.Should().BeTrue();
                stream.ReceivedCalls().Should().BeEmpty();
            }
        }
    }
}
