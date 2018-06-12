namespace Host.UnitTests.Conversion
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Crest.Core;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class FileDataFactoryTests
    {
        private const string BoundaryText = "boundary.text";
        private const string NewLine = "\r\n";
        private readonly ArrayPool<byte> bytePool;
        private readonly FileDataFactory factory;

        private FileDataFactoryTests()
        {
            this.bytePool = Substitute.For<ArrayPool<byte>>();
            this.bytePool.Rent(0).ReturnsForAnyArgs(ci => new byte[ci.Arg<int>()]);

            this.factory = new FileDataFactory(this.bytePool);
        }

        public sealed class CanRead : FileDataFactoryTests
        {
            [Fact]
            public void ShouldReturnTrue()
            {
                this.factory.CanRead
                    .Should().BeTrue();
            }
        }

        public sealed class CanWrite : FileDataFactoryTests
        {
            [Fact]
            public void ShouldReturnFalse()
            {
                this.factory.CanWrite
                    .Should().BeFalse();
            }
        }

        public sealed class ContentType : FileDataFactoryTests
        {
            [Fact]
            public void ShouldThrowNotSupportedException()
            {
                this.factory.Invoking(x => _ = x.ContentType)
                    .Should().Throw<NotSupportedException>();
            }
        }

        public sealed class Formats : FileDataFactoryTests
        {
            [Fact]
            public void ShouldIncludeAnyMultipartType()
            {
                this.factory.Formats.Should().Contain(
                    "multipart/*");
            }
        }

        public sealed class Prime : FileDataFactoryTests
        {
            [Fact]
            public void ShouldNotThrowAnException()
            {
                this.factory.Invoking(x => x.Prime(typeof(int)))
                    .Should().NotThrow();
            }
        }

        public sealed class Priority : FileDataFactoryTests
        {
            [Fact]
            public void ShouldReturnAPositiveNumber()
            {
                this.factory.Priority.Should().BePositive();
            }
        }

        public sealed class ReadFrom : FileDataFactoryTests
        {
            [Fact]
            public void ShouldReturnAnEmptyArrayIfNoBoundaryHeaderIsPresent()
            {
                object result = this.factory.ReadFrom(
                    new Dictionary<string, string>(),
                    null,
                    typeof(IFileData[]));

                result.Should().BeAssignableTo<IFileData[]>().Which.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnAnEmptyArrayIfNotAMultipartType()
            {
                IReadOnlyDictionary<string, string> headers = CreateContentType("text/plain;boundary=" + BoundaryText);

                object result = this.factory.ReadFrom(
                    headers,
                    null,
                    typeof(IFileData[]));

                result.Should().BeAssignableTo<IFileData[]>().Which.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnForIEnumerableTypes()
            {
                const string Body =
                    "--" + BoundaryText + NewLine +
                    NewLine +
                    "First part" + NewLine +
                    "--" + BoundaryText + NewLine +
                    NewLine +
                    "Second part" + NewLine +
                    "--" + BoundaryText + "--";

                object result = this.ReadBody(Body, typeof(IEnumerable<IFileData>));

                result.Should().BeAssignableTo<IEnumerable<IFileData>>()
                      .Which.Should().HaveCount(2);
            }

            [Fact]
            public void ShouldReturnMultipleParts()
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

                IFileData[] result = this.GetFiles(Body);

                result.Should().HaveCount(2);
                Encoding.ASCII.GetString(result[0].Contents).Should().Be("First part");
                Encoding.ASCII.GetString(result[1].Contents).Should().Be("Second part");
            }

            [Fact]
            public void ShouldReturnNullForSingleParameterTypesWhenThereAreNoFiles()
            {
                object result = this.factory.ReadFrom(
                    new Dictionary<string, string>(),
                    null,
                    typeof(IFileData));

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTextContentTypeWhenNoneIsSpecified()
            {
                const string Body =
                    "--" + BoundaryText + NewLine +
                    NewLine +
                    "Part" + NewLine +
                    "--" + BoundaryText + "--";

                IFileData[] result = this.GetFiles(Body);

                result.Should().ContainSingle().Which
                      .ContentType.Should().Be("text/plain");
            }

            [Fact]
            public void ShouldReturnTheFilename()
            {
                const string Body =
                    "--" + BoundaryText + NewLine +
                    "Content-Disposition: inline; filename=\"myFile.txt\"" + NewLine +
                    NewLine +
                    "Part" + NewLine +
                    "--" + BoundaryText + "--";

                IFileData[] result = this.GetFiles(Body);

                result.Should().ContainSingle().Which
                      .Filename.Should().Be("myFile.txt");
            }

            [Fact]
            public void ShouldReturnTheFirstFileForSingleParameters()
            {
                const string Body =
                    "--" + BoundaryText + NewLine +
                    "Content-Disposition: inline; filename=\"first.txt\"" + NewLine +
                    NewLine +
                    NewLine +
                    "--" + BoundaryText + NewLine +
                    "Content-Disposition: inline; filename=\"second.txt\"" + NewLine +
                    NewLine +
                    NewLine +
                    "--" + BoundaryText + "--";

                object result = this.ReadBody(Body, typeof(IFileData));

                result.Should().BeAssignableTo<IFileData>()
                      .Which.Filename.Should().Be("first.txt");
            }

            [Fact]
            public void ShouldReturnTheHeaders()
            {
                const string HeaderName = "Content-Disposition";
                const string HeaderValue = "inline; filesize=1024";
                const string Body =
                    "--" + BoundaryText + NewLine +
                    HeaderName + ":" + HeaderValue + NewLine +
                    NewLine +
                    "Part" + NewLine +
                    "--" + BoundaryText + "--";

                IFileData[] result = this.GetFiles(Body);

                result.Should().ContainSingle().Which
                      .Headers[HeaderName].Should().Be(HeaderValue);
            }

            private static Dictionary<string, string> CreateContentType(string value)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Content-Type"] = value
                };
            }

            private IFileData[] GetFiles(string body)
            {
                return (IFileData[])this.ReadBody(body, typeof(IFileData[]));
            }

            private object ReadBody(string body, Type type)
            {
                IReadOnlyDictionary<string, string> headers = CreateContentType("multipart/mixed; boundary=" + BoundaryText);

                byte[] bytes = Encoding.ASCII.GetBytes(body);
                using (var stream = new MemoryStream(bytes, writable: false))
                {
                    return this.factory.ReadFrom(
                        headers,
                        stream,
                        type);
                }
            }
        }
    }
}
