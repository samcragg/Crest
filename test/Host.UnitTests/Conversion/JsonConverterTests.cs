namespace Host.UnitTests.Conversion
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Conversion;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class JsonConverterTests
    {
        private readonly JsonConverter converter;
        private readonly ISerializerGenerator<JsonSerializerBase> serializer =
            Substitute.For<ISerializerGenerator<JsonSerializerBase>>();

        private JsonConverterTests()
        {
            this.converter = new JsonConverter(this.serializer);
        }

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

        public sealed class Prime : JsonConverterTests
        {
            [Fact]
            public void ShouldPrimeTheSerializerGenerator()
            {
                this.converter.Prime(typeof(int));

                this.serializer.Received().GetSerializerFor(typeof(int));
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
            public void ShouldSerializeTheValue()
            {
                var instance = new SimpleObject();

                this.converter.WriteTo(Stream.Null, instance);

                this.serializer.Received().Serialize(Stream.Null, instance);
            }

            private class SimpleObject
            {
                public int IntegerProperty { get; set; }
            }
        }
    }
}
