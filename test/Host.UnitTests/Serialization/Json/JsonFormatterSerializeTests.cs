namespace Host.UnitTests.Serialization.Json
{
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization.Internal;
    using Crest.Host.Serialization.Json;
    using FluentAssertions;
    using Xunit;

    public class JsonFormatterSerializeTests
    {
        private readonly JsonFormatter formatter;
        private readonly MemoryStream stream = new MemoryStream();

        protected JsonFormatterSerializeTests()
        {
            this.formatter = new JsonFormatter(this.stream, SerializationMode.Serialize);
        }

        protected byte[] GetWrittenData()
        {
            this.formatter.Writer.Flush();
            return this.stream.ToArray();
        }

        public sealed class EnumsAsIntegers : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldReturnTrue()
            {
                // This matches the JSON.NET default
                this.formatter.EnumsAsIntegers.Should().BeTrue();
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
                var result = (byte[])JsonFormatter.GetMetadata(
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

        public sealed class WriteBeginArray : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteTheOpeningBracket()
            {
                this.formatter.WriteBeginArray(null, 0);
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)'[');
            }
        }

        public sealed class WriteBeginClass : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteTheOpeningBraceForByteArrays()
            {
                this.formatter.WriteBeginClass((byte[])null);
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)'{');
            }

            [Fact]
            public void ShouldWriteTheOpeningBraceForStrings()
            {
                this.formatter.WriteBeginClass((string)null);
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)'{');
            }
        }

        public sealed class WriteBeginPrimitive : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.formatter.Invoking(x => x.WriteBeginPrimitive(null))
                    .Should().NotThrow();
            }
        }

        public sealed class WriteBeginProperty : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteACommaBetweenProperties()
            {
                byte[] data = new byte[0];

                this.formatter.WriteBeginProperty(data);
                this.formatter.WriteBeginProperty(data);
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)',');
            }

            [Fact]
            public void ShouldWriteTheMetadata()
            {
                this.formatter.WriteBeginProperty(new byte[] { 1, 2 });
                byte[] written = this.GetWrittenData();

                written.Should().Equal(1, 2);
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.formatter.WriteBeginProperty("Aa");
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)'"', (byte)'a', (byte)'a', (byte)'"', (byte)':');
            }
        }

        public sealed class WriteElementSeparator : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteAComma()
            {
                this.formatter.WriteElementSeparator();
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)',');
            }
        }

        public sealed class WriteEndArray : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteTheClosingBracket()
            {
                this.formatter.WriteEndArray();
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)']');
            }
        }

        public sealed class WriteEndClass : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteTheClosingBrace()
            {
                this.formatter.WriteEndClass();
                byte[] written = this.GetWrittenData();

                written.Should().Equal((byte)'}');
            }
        }

        public sealed class WriteEndPrimitive : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.formatter.Invoking(x => x.WriteEndPrimitive())
                    .Should().NotThrow();
            }
        }

        public sealed class WriteEndProperty : JsonFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.formatter.Invoking(x => x.WriteEndProperty())
                    .Should().NotThrow();
            }
        }
    }
}
