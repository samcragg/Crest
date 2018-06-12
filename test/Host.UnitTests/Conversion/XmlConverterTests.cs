namespace Host.UnitTests.Conversion
{
    using System.IO;
    using Crest.Host.Conversion;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class XmlConverterTests
    {
        private readonly XmlConverter converter;

        private readonly ISerializerGenerator<XmlSerializerBase> serializer =
            Substitute.For<ISerializerGenerator<XmlSerializerBase>>();

        private XmlConverterTests()
        {
            this.converter = new XmlConverter(this.serializer);
        }

        public sealed class CanRead : XmlConverterTests
        {
            [Fact]
            public void ShouldReturnTrue()
            {
                this.converter.CanRead
                    .Should().BeTrue();
            }
        }

        public sealed class CanWrite : XmlConverterTests
        {
            [Fact]
            public void ShouldReturnTrue()
            {
                this.converter.CanWrite
                    .Should().BeTrue();
            }
        }

        public sealed class ContentType : XmlConverterTests
        {
            [Fact]
            public void ShouldBeTheIanaXmlMimeType()
            {
                // https://www.iana.org/assignments/media-types/application/xml
                this.converter.ContentType.Should().Be("application/xml");
            }
        }

        public sealed class Formats : XmlConverterTests
        {
            [Fact]
            public void ShouldIncludeAllTheIanaXmlMimeType()
            {
                // https://www.iana.org/assignments/media-types/application/xml
                // https://www.iana.org/assignments/media-types/text/xml
                this.converter.Formats.Should().Contain(
                    new[] { "application/xml", "text/xml" });
            }
        }

        public sealed class Prime : XmlConverterTests
        {
            [Fact]
            public void ShouldPrimeTheSerializerGenerator()
            {
                this.converter.Prime(typeof(int));

                this.serializer.Received().GetSerializerFor(typeof(int));
            }
        }

        public sealed class Priority : XmlConverterTests
        {
            [Fact]
            public void ShouldReturnAPositiveNumber()
            {
                this.converter.Priority.Should().BePositive();
            }
        }

        public sealed class ReadFrom : XmlConverterTests
        {
            [Fact]
            public void ShouldDeserializeTheValue()
            {
                var instance = new SimpleObject();
                this.serializer.Deserialize(Stream.Null, typeof(SimpleObject))
                    .Returns(instance);

                object result = this.converter.ReadFrom(null, Stream.Null, typeof(SimpleObject));

                result.Should().BeSameAs(instance);
            }
        }

        public sealed class WriteTo : XmlConverterTests
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
            public void ShouldSerializeTheValue()
            {
                var instance = new SimpleObject();

                this.converter.WriteTo(Stream.Null, instance);

                this.serializer.Received().Serialize(Stream.Null, instance);
            }
        }

        private class SimpleObject
        {
            public int IntegerProperty { get; set; }
        }
    }
}
