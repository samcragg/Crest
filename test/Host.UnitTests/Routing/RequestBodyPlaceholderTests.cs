namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.IO;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class RequestBodyPlaceholderTests
    {
        private readonly IContentConverter converter = Substitute.For<IContentConverter>();
        private readonly RequestBodyPlaceholder placeholder = new RequestBodyPlaceholder(typeof(int));
        private readonly BlockStreamPool streamPool = Substitute.For<BlockStreamPool>();

        private RequestBodyPlaceholderTests()
        {
            this.streamPool.GetStream().Returns(_ => new MemoryStream());
        }

        public sealed class UpdateRequestAsyncTests : RequestBodyPlaceholderTests
        {
            [Fact]
            public async Task ShouldBufferTheStream()
            {
                IRequestData request = Substitute.For<IRequestData>();
                request.Body.Returns(new NonSeekableStream("Data"));

                await this.placeholder.UpdateRequestAsync(
                    this.converter,
                    this.streamPool,
                    request);

                this.converter.Received().ReadFrom(
                    Arg.Any<IReadOnlyDictionary<string, string>>(),
                    Arg.Is<Stream>(s => GetStreamContentsAsString(s) == "Data"),
                    typeof(int));
            }

            [Fact]
            public async Task ShouldUpdateTheValueFromTheConverter()
            {
                IRequestData request = Substitute.For<IRequestData>();
                request.Body.Returns(new MemoryStream());
                request.Headers.Returns(new Dictionary<string, string>());

                object converterValue = new object();
                this.converter.ReadFrom(request.Headers, request.Body, typeof(int))
                    .Returns(converterValue);

                await this.placeholder.UpdateRequestAsync(
                    this.converter,
                    this.streamPool,
                    request);

                this.placeholder.Value.Should().BeSameAs(converterValue);
            }

            private static string GetStreamContentsAsString(Stream stream)
            {
                byte[] buffer = ((MemoryStream)stream).ToArray();
                return Encoding.UTF8.GetString(buffer);
            }

            private class NonSeekableStream : MemoryStream
            {
                public NonSeekableStream(string data)
                    : base(Encoding.UTF8.GetBytes(data), writable: false)
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
