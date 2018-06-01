namespace Host.UnitTests.IO
{
    using System.IO;
    using Crest.Host.IO;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class IOUtilsTests
    {
        public sealed class ReadBytes : IOUtilsTests
        {
            [Fact]
            public void ShouldOnlyReadTheSpecifiedNumberOfBytes()
            {
                var stream = Substitute.For<Stream>();
                stream.Read(null, 0, 0).ReturnsForAnyArgs(3);

                int result = IOUtils.ReadBytes(stream, new byte[10], 3);

                result.Should().Be(3);
            }

            [Fact]
            public void ShouldReadFromTheStreamMultipleTimes()
            {
                byte[] buffer = new byte[8];
                var stream = Substitute.For<Stream>();
                stream.Read(buffer, 0, 8).Returns(5);
                stream.Read(buffer, 5, 3).Returns(3);

                int result = IOUtils.ReadBytes(stream, buffer, 8);

                result.Should().Be(8);
            }

            [Fact]
            public void ShouldReturnTheNumberOfBytesRead()
            {
                var stream = Substitute.For<Stream>();
                stream.Read(null, 0, 0).ReturnsForAnyArgs(10, 0);

                int result = IOUtils.ReadBytes(stream, new byte[20], 20);

                result.Should().Be(10);
            }
        }
    }
}