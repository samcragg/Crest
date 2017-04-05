namespace Host.UnitTests.Conversion
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class JsonConverterTests
    {
        private readonly JsonConverter converter = new JsonConverter();

        public sealed class ContentType : JsonConverterTests
        {
            [Fact]
            public void ShouldBeTheIanaJsonMimeType()
            {
                // http://www.iana.org/assignments/media-types/application/json
                this.converter.ContentType.Should().Be("application/json");
            }
        }

        public sealed class Formats : JsonConverterTests
        {
            [Fact]
            public void ShouldIncludeTheIanaJsonMimeType()
            {
                // http://www.iana.org/assignments/media-types/application/json
                this.converter.Formats.Should().Contain("application/json");
            }
        }

        public sealed class Priority : JsonConverterTests
        {
            [Fact]
            public void ShouldReturnAPositiveNumber()
            {
                this.converter.Priority.Should().BePositive();
            }
        }

        public sealed class WriteTo : JsonConverterTests
        {
            [Fact]
            public void MustNotDisposeTheStream()
            {
                Stream stream = Substitute.For<Stream>();
                stream.CanWrite.Returns(true);

                this.converter.WriteTo(stream, "value");

                stream.DidNotReceive().Dispose();
            }

            [Fact]
            public void ShouldEncodeWithUtf8()
            {
                using (var stream = new MemoryStream())
                {
                    // ¶ (U+00B6) is a single UTF16 character but is encoded in
                    // UTF-8 as 0xC2 0xB6
                    this.converter.WriteTo(stream, new SimpleObject { StringProperty = "¶" });

                    byte[] bytes = stream.ToArray();

                    int start = Array.IndexOf(bytes, (byte)0xc2);
                    bytes[start + 1].Should().Be(0xB6);
                }
            }

            [Fact]
            public void ShouldUseCamelCaseForProperties()
            {
                using (var stream = new MemoryStream())
                {
                    this.converter.WriteTo(stream, new SimpleObject { IntegerProperty = 123 });

                    string data = Encoding.UTF8.GetString(stream.ToArray());

                    data.Should().Contain("integerProperty");
                }
            }

            private class SimpleObject
            {
                public int IntegerProperty { get; set; }

                public string StringProperty { get; set; }
            }
        }
    }
}
