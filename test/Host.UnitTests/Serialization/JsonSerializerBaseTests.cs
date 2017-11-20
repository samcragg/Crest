namespace Host.UnitTests.Serialization
{
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class JsonSerializerBaseTests
    {
        private readonly JsonSerializerBase serializer;
        private readonly MemoryStream stream = new MemoryStream();

        protected JsonSerializerBaseTests()
        {
            this.serializer = new FakeJsonSerializerBase(this.stream, SerializationMode.Serialize);
        }

        protected byte[] GetWrittenData()
        {
            this.serializer.Flush();
            return this.stream.ToArray();
        }

        public sealed class Constructor : JsonSerializerBaseTests
        {
            [Fact]
            public void ShouldCreateAStreamWriter()
            {
                var instance = new FakeJsonSerializerBase(Stream.Null, SerializationMode.Serialize);

                instance.Writer.Should().NotBeNull();
            }

            [Fact]
            public void ShouldSetTheWriter()
            {
                var parent = new FakeJsonSerializerBase(Stream.Null, SerializationMode.Serialize);

                var instance = new FakeJsonSerializerBase(parent);

                instance.Writer.Should().BeSameAs(parent.Writer);
            }
        }

        public sealed class Flush : JsonSerializerBaseTests
        {
            [Fact]
            public void ShouldFlushTheBuffers()
            {
                Stream stream = Substitute.For<Stream>();
                JsonSerializerBase serializer = new FakeJsonSerializerBase(stream, SerializationMode.Serialize);

                serializer.WriteBeginClass(null);
                stream.DidNotReceiveWithAnyArgs().Write(null, 0, 0);

                serializer.Flush();
                stream.ReceivedWithAnyArgs().Write(null, 0, 0);
            }
        }

        public sealed class GetMetadata : JsonSerializerBaseTests
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

        public sealed class OutputEnumNames : JsonSerializerBaseTests
        {
            [Fact]
            public void ShouldReturnFalse()
            {
                // This matches the JSON.NET default
                JsonSerializerBase.OutputEnumNames.Should().BeFalse();
            }
        }

        public sealed class WriteBeginArray : JsonSerializerBaseTests
        {
            [Fact]
            public void ShouldWriteTheOpeningBracket()
            {
                this.serializer.WriteBeginArray(null, 0);
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)'[');
            }
        }

        public sealed class WriteBeginClass : JsonSerializerBaseTests
        {
            [Fact]
            public void ShouldWriteTheOpeningBrace()
            {
                this.serializer.WriteBeginClass(null);
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)'{');
            }
        }

        public sealed class WriteBeginProperty : JsonSerializerBaseTests
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
        }

        public sealed class WriteElementSeparator : JsonSerializerBaseTests
        {
            [Fact]
            public void ShouldWriteAComma()
            {
                this.serializer.WriteElementSeparator();
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)',');
            }
        }

        public sealed class WriteEndArray : JsonSerializerBaseTests
        {
            [Fact]
            public void ShouldWriteTheClosingBracket()
            {
                this.serializer.WriteEndArray();
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)']');
            }
        }

        public sealed class WriteEndClass : JsonSerializerBaseTests
        {
            [Fact]
            public void ShouldWriteTheClosingBrace()
            {
                this.serializer.WriteEndClass();
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)'}');
            }
        }

        private sealed class FakeJsonSerializerBase : JsonSerializerBase
        {
            public FakeJsonSerializerBase(Stream stream, SerializationMode mode) : base(stream, mode)
            {
            }

            public FakeJsonSerializerBase(JsonSerializerBase parent) : base(parent)
            {
            }
        }
    }
}
