namespace Host.UnitTests.Serialization.Json
{
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class JsonFormatterSerializeTests
    {
        private readonly JsonSerializerBase serializer;
        private readonly MemoryStream stream = new MemoryStream();

        protected JsonFormatterSerializeTests()
        {
            this.serializer = new FakeJsonSerializerBase(this.stream);
        }

        protected byte[] GetWrittenData()
        {
            this.serializer.Flush();
            return this.stream.ToArray();
        }

        public sealed class BeginWrite : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.serializer.Invoking(x => x.BeginWrite(null))
                    .Should().NotThrow();
            }
        }

        public sealed class Constructor : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldCreateAStreamWriter()
            {
                var instance = new FakeJsonSerializerBase(Stream.Null);

                instance.Writer.Should().NotBeNull();
            }

            [Fact]
            public void ShouldSetTheWriter()
            {
                var parent = new FakeJsonSerializerBase(Stream.Null);

                var instance = new FakeJsonSerializerBase(parent);

                instance.Writer.Should().BeSameAs(parent.Writer);
            }
        }

        public sealed class EndWrite : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.serializer.Invoking(x => x.EndWrite())
                    .Should().NotThrow();
            }
        }

        public sealed class Flush : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldFlushTheBuffers()
            {
                Stream stream = Substitute.For<Stream>();
                JsonSerializerBase serializer = new FakeJsonSerializerBase(stream);

                serializer.WriteBeginClass((byte[])null);
                stream.DidNotReceiveWithAnyArgs().Write(null, 0, 0);

                serializer.Flush();
                stream.ReceivedWithAnyArgs().Write(null, 0, 0);
            }
        }

        public sealed class GetMetadata : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldChangeTheCaseOfTheProperty()
            {
                string property = GetPropertyNameFromMetadata("SimpleProperty");

                property.Should().Be("simpleProperty");
            }

            [Fact]
            public void ShouldEscapeCharactersFromTheDisplayName()
            {
                string property = GetPropertyNameFromMetadata("HasSpecialCharacters");

                property.Should().Be(@"\"" \\ \u0001");
            }

            [Fact]
            public void ShouldUseTheDisplayNameAttribute()
            {
                string property = GetPropertyNameFromMetadata("HasDisplayName");

                property.Should().Be("displayValue");
            }

            private static string GetPropertyNameFromMetadata(string property)
            {
                byte[] result = JsonSerializerBase.GetMetadata(
                    typeof(ExampleProperties).GetProperty(property));

                // The returned value will start with a " and end with a ": but
                // we only want the part in the middle
                return Encoding.UTF8.GetString(result, 1, result.Length - 3);
            }

            private class ExampleProperties
            {
                [DisplayName("displayValue")]
                public int HasDisplayName { get; set; }

                [DisplayName("\" \\ \x01")]
                public int HasSpecialCharacters { get; set; }

                public int SimpleProperty { get; set; }
            }
        }

        public sealed class OutputEnumNames : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldReturnFalse()
            {
                // This matches the JSON.NET default
                JsonSerializerBase.OutputEnumNames.Should().BeFalse();
            }
        }

        public sealed class WriteBeginArray : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteTheOpeningBracket()
            {
                this.serializer.WriteBeginArray(null, 0);
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)'[');
            }
        }

        public sealed class WriteBeginClass : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteTheOpeningBraceForByteArrays()
            {
                this.serializer.WriteBeginClass((byte[])null);
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)'{');
            }

            [Fact]
            public void ShouldWriteTheOpeningBraceForStrings()
            {
                this.serializer.WriteBeginClass((string)null);
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)'{');
            }
        }

        public sealed class WriteBeginProperty : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteACommaBetweenProperties()
            {
                byte[] data = new byte[0];

                this.serializer.WriteBeginProperty(data);
                this.serializer.WriteBeginProperty(data);
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)',');
            }

            [Fact]
            public void ShouldWriteTheMetadata()
            {
                this.serializer.WriteBeginProperty(new byte[] { 1, 2 });
                byte[] written = this.GetWrittenData();

                written.Should().Equal(1, 2);
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.serializer.WriteBeginProperty("Aa");
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)'"', (byte)'a', (byte)'a', (byte)'"', (byte)':');
            }
        }

        public sealed class WriteElementSeparator : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteAComma()
            {
                this.serializer.WriteElementSeparator();
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)',');
            }
        }

        public sealed class WriteEndArray : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteTheClosingBracket()
            {
                this.serializer.WriteEndArray();
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)']');
            }
        }

        public sealed class WriteEndClass : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteTheClosingBrace()
            {
                this.serializer.WriteEndClass();
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)'}');
            }
        }

        public sealed class WriteEndProperty : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.serializer.Invoking(x => x.WriteEndProperty())
                    .Should().NotThrow();
            }
        }

        private sealed class FakeJsonSerializerBase : JsonSerializerBase
        {
            public FakeJsonSerializerBase(Stream stream) : base(stream, SerializationMode.Serialize)
            {
            }

            public FakeJsonSerializerBase(JsonSerializerBase parent) : base(parent)
            {
            }
        }
    }
}
