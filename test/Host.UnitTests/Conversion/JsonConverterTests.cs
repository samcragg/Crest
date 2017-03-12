namespace Host.UnitTests.Conversion
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class JsonConverterTests
    {
        private JsonConverter converter;

        [SetUp]
        public void SetUp()
        {
            this.converter = new JsonConverter();
        }

        [TestFixture]
        public sealed class ContentType : JsonConverterTests
        {
            [Test]
            public void ShouldBeTheIanaJsonMimeType()
            {
                // http://www.iana.org/assignments/media-types/application/json
                this.converter.ContentType.Should().Be("application/json");
            }
        }

        [TestFixture]
        public sealed class Formats : JsonConverterTests
        {
            [Test]
            public void ShouldIncludeTheIanaJsonMimeType()
            {
                // http://www.iana.org/assignments/media-types/application/json
                this.converter.Formats.Should().Contain("application/json");
            }
        }

        [TestFixture]
        public sealed class Priority : JsonConverterTests
        {
            [Test]
            public void ShouldReturnAPositiveNumber()
            {
                this.converter.Priority.Should().BePositive();
            }
        }

        [TestFixture]
        public sealed class WriteTo : JsonConverterTests
        {
            [Test]
            public void MustNotDisposeTheStream()
            {
                Stream stream = Substitute.For<Stream>();
                stream.CanWrite.Returns(true);

                this.converter.WriteTo(stream, "value");

                stream.DidNotReceive().Dispose();
            }

            [Test]
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

            [Test]
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
