namespace Host.UnitTests
{
    using System.IO;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class ResponseDataTests
    {
        public sealed class Headers : ResponseDataTests
        {
            private readonly ResponseData data = new ResponseData("", 0, null);

            [Fact]
            public void ExplicitInterfaceShouldReturnTheSameValues()
            {
                this.data.Headers.Add("Key", "Value");

                IResponseData responseData = this.data;
                responseData.Headers["Key"].Should().Be("Value");
            }

            [Fact]
            public void ShouldReturnAMutableDictionary()
            {
                this.data.Headers.Add("Key", "Value");

                this.data.Headers["Key"].Should().Be("Value");
            }
        }

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
                Stream stream = Substitute.For<Stream>();

                Task write = data.WriteBody(stream);

                write.Should().NotBeNull();
                write.IsCompleted.Should().BeTrue();
                stream.ReceivedCalls().Should().BeEmpty();
            }
        }
    }
}
