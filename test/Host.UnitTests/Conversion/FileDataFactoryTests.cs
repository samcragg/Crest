namespace Host.UnitTests.Conversion
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Core;
    using Crest.Host.Conversion;
    using Crest.Host.IO;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class FileDataFactoryTests
    {
        private const string BoundaryText = "boundary.text";
        private const string NewLine = "\r\n";
        private readonly ArrayPool<byte> bytePool;
        private readonly FileDataFactory factory;
        private readonly BlockStreamPool streamPool;

        private FileDataFactoryTests()
        {
            this.bytePool = Substitute.For<ArrayPool<byte>>();
            this.bytePool.Rent(0).ReturnsForAnyArgs(ci => new byte[ci.Arg<int>()]);

            this.streamPool = Substitute.For<BlockStreamPool>();
            this.streamPool.GetStream().Returns(_ => new MemoryStream());

            this.factory = new FileDataFactory(this.bytePool, this.streamPool);
        }

        private static IRequestData CreateRequest(string body)
        {
            IRequestData request = Substitute.For<IRequestData>();
            SetContentType(request, "multipart/mixed; boundary=" + BoundaryText);

            byte[] bytes = Encoding.ASCII.GetBytes(body);
            request.Body.Returns(new MemoryStream(bytes, writable: false));

            return request;
        }

        private static void SetContentType(IRequestData request, string value)
        {
            request.Headers.Returns(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = value
            });
        }

        public sealed class CreateFilesAsync : FileDataFactoryTests
        {
            [Fact]
            public void ShouldBufferTheStream()
            {
                const string Body =
                    "--" + BoundaryText + NewLine +
                    NewLine +
                    "Part" + NewLine +
                    "--" + BoundaryText + "--";

                IRequestData request = Substitute.For<IRequestData>();
                SetContentType(request, "multipart/mixed; boundary=" + BoundaryText);
                request.Body.Returns(new NonSeekableStream(Body));

                Func<Task> action = () => this.factory.CreateFilesAsync(request);

                action.Should().NotThrow();
            }

            [Fact]
            public async Task ShouldReturnAnEmptyArrayIfNoBoundaryHeaderIsPresent()
            {
                IRequestData request = Substitute.For<IRequestData>();

                IFileData[] result = await this.factory.CreateFilesAsync(request);

                result.Should().BeEmpty();
            }

            [Fact]
            public async Task ShouldReturnAnEmptyArrayIfNotAMultipartType()
            {
                IRequestData request = Substitute.For<IRequestData>();
                SetContentType(request, "text/plain;boundary=" + BoundaryText);

                IFileData[] result = await this.factory.CreateFilesAsync(request);

                result.Should().BeEmpty();
            }

            [Fact]
            public async Task ShouldReturnMultipleParts()
            {
                const string Body =
                    "--" + BoundaryText + NewLine +
                    "Content-type: text/plain" + NewLine +
                    NewLine +
                    "First part" + NewLine +
                    "--" + BoundaryText + NewLine +
                    "Content-type: text/plain" + NewLine +
                    NewLine +
                    "Second part" + NewLine +
                    "--" + BoundaryText + "--";

                IFileData[] result = await this.factory.CreateFilesAsync(
                    CreateRequest(Body));

                result.Should().HaveCount(2);
                Encoding.ASCII.GetString(result[0].Contents).Should().Be("First part");
                Encoding.ASCII.GetString(result[1].Contents).Should().Be("Second part");
            }

            [Fact]
            public async Task ShouldReturnTextContentTypeWhenNoneIsSpecified()
            {
                const string Body =
                    "--" + BoundaryText + NewLine +
                    NewLine +
                    "Part" + NewLine +
                    "--" + BoundaryText + "--";

                IFileData[] result = await this.factory.CreateFilesAsync(
                    CreateRequest(Body));

                result.Should().ContainSingle().Which
                      .ContentType.Should().Be("text/plain");
            }

            [Fact]
            public async Task ShouldReturnTheFilename()
            {
                const string Body =
                    "--" + BoundaryText + NewLine +
                    "Content-Disposition: inline; filename=\"myFile.txt\"" + NewLine +
                    NewLine +
                    "Part" + NewLine +
                    "--" + BoundaryText + "--";

                IFileData[] result = await this.factory.CreateFilesAsync(
                    CreateRequest(Body));

                result.Should().ContainSingle().Which
                      .Filename.Should().Be("myFile.txt");
            }

            [Fact]
            public async Task ShouldReturnTheHeaders()
            {
                const string HeaderName = "Content-Disposition";
                const string HeaderValue = "inline; filesize=1024";
                const string Body =
                    "--" + BoundaryText + NewLine +
                    HeaderName + ":" + HeaderValue + NewLine +
                    NewLine +
                    "Part" + NewLine +
                    "--" + BoundaryText + "--";

                IFileData[] result = await this.factory.CreateFilesAsync(
                    CreateRequest(Body));

                result.Should().ContainSingle().Which
                      .Headers[HeaderName].Should().Be(HeaderValue);
            }

            private class NonSeekableStream : MemoryStream
            {
                public NonSeekableStream(string data)
                    : base(Encoding.ASCII.GetBytes(data), writable: false)
                {
                }

                public override bool CanSeek => false;

                public override long Position
                {
                    get => throw new NotSupportedException();
                    set => throw new NotSupportedException();
                }

                public override long Seek(long offset, SeekOrigin loc)
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
