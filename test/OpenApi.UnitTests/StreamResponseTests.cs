namespace OpenApi.UnitTests
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.OpenApi;
    using FluentAssertions;
    using Xunit;

    public class StreamResponseTests : IDisposable
    {
        private readonly StreamResponse response;
        private readonly MemoryStream source = new MemoryStream();

        public StreamResponseTests()
        {
            this.response = new StreamResponse(() => this.source);
        }

        public void Dispose()
        {
            this.source.Dispose();
        }

        public sealed class Compression : StreamResponseTests
        {
            [Fact]
            public Task ShouldSendDeflateResponses()
            {
                return TestCompression(
                    "deflate",
                    s => new DeflateStream(s, CompressionMode.Decompress));
            }

            [Fact]
            public Task ShouldSendGzipResponses()
            {
                return TestCompression(
                    "gzip",
                    s => new GZipStream(s, CompressionMode.Decompress));
            }

            [Fact]
            public Task ShouldSendUncompressedResponses()
            {
                return TestCompression(
                    null,
                    s => s);
            }

            private static async Task TestCompression(string encoding, Func<Stream, Stream> decompress)
            {
                Assembly assembly = typeof(StreamResponseTests).GetTypeInfo().Assembly;
                var response = new StreamResponse(
                    () => assembly.GetManifestResourceStream("OpenApi.UnitTests.TestData.HelloWorld.gz"),
                    encoding ?? string.Empty);

                response.Headers.TryGetValue("Content-Encoding", out string contentEncoding);
                contentEncoding.Should().Be(encoding);

                using (var buffer = new MemoryStream())
                {
                    await response.WriteBody(buffer);

                    buffer.Position = 0;
                    using (var reader = new StreamReader(decompress(buffer)))
                    {
                        string result = reader.ReadToEnd();
                        result.Should().Be("Hello World!");
                    }
                }
            }
        }

        public sealed class Headers : StreamResponseTests
        {
            [Fact]
            public void ShouldBeEmpty()
            {
                this.response.Headers.Should().BeEmpty();
            }
        }

        public sealed class StatusCode : StreamResponseTests
        {
            [Fact]
            public void ShouldReturnOk()
            {
                this.response.StatusCode.Should().Be(200);
            }
        }

        public sealed class WriteBody : StreamResponseTests
        {
            [Fact]
            public async Task ShouldCopyTheStream()
            {
                byte[] data = { 1, 2, 3 };
                this.source.Write(data, 0, data.Length);
                this.source.Position = 0;

                using (var destination = new MemoryStream())
                {
                    await this.response.WriteBody(destination);

                    destination.ToArray().Should().BeSubsetOf(data);
                }
            }
        }
    }
}
