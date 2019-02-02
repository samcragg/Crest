namespace Host.UnitTests.Serialization.UrlEncoded
{
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization.Internal;
    using Crest.Host.Serialization.UrlEncoded;
    using FluentAssertions;
    using Xunit;

    public class UrlEncodedFormatterSerializeTests
    {
        private readonly UrlEncodedFormatter formatter;
        private readonly MemoryStream stream = new MemoryStream();

        protected UrlEncodedFormatterSerializeTests()
        {
            this.formatter = new UrlEncodedFormatter(this.stream, SerializationMode.Serialize);
        }

        protected byte[] GetWrittenData()
        {
            this.formatter.Writer.Flush();
            return this.stream.ToArray();
        }

        public sealed class EnumsAsIntegers : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldReturnFalse()
            {
                // Try to match XML, which uses the names
                this.formatter.EnumsAsIntegers.Should().BeFalse();
            }
        }

        public sealed class GetMetadata : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldEscapeCharactersFromTheDisplayName()
            {
                string property = GetPropertyNameFromMetadata("HasSpecialCharacters");

                property.Should().Be("A%3DB");
            }

            [Fact]
            public void ShouldReturnThePropertyName()
            {
                string property = GetPropertyNameFromMetadata("SimpleProperty");

                property.Should().Be("SimpleProperty");
            }

            [Fact]
            public void ShouldUseTheDisplayNameAttribute()
            {
                string property = GetPropertyNameFromMetadata("HasDisplayName");

                property.Should().Be("DisplayValue");
            }

            private static string GetPropertyNameFromMetadata(string property)
            {
                var result = (byte[])UrlEncodedFormatter.GetMetadata(
                    typeof(ExampleProperties).GetProperty(property));

                return Encoding.UTF8.GetString(result, 0, result.Length);
            }

            private class ExampleProperties
            {
                [DisplayName("DisplayValue")]
                public int HasDisplayName { get; set; }

                [DisplayName("A=B")]
                public int HasSpecialCharacters { get; set; }

                public int SimpleProperty { get; set; }
            }
        }

        public sealed class WriteBeginArray : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteTheZeroIndex()
            {
                this.formatter.WriteBeginArray(typeof(int[]), 1);
                this.formatter.Writer.WriteString(string.Empty);

                this.GetWrittenData().Should().Equal((byte)'0', (byte)'=');
            }
        }

        public sealed class WriteBeginClass : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyExceptionForByteArrays()
            {
                this.formatter.Invoking(x => x.WriteBeginClass((byte[])null))
                    .Should().NotThrow();
            }

            [Fact]
            public void ShouldNotThrowAnyExceptionForStrings()
            {
                this.formatter.Invoking(x => x.WriteBeginClass((string)null))
                    .Should().NotThrow();
            }
        }

        public sealed class WriteBeginPrimitive : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.formatter.Invoking(x => x.WriteBeginPrimitive(null))
                    .Should().NotThrow();
            }
        }

        public sealed class WriteBeginProperty : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteTheMetadata()
            {
                this.formatter.WriteBeginProperty(new byte[] { 1, 2 });
                this.formatter.Writer.WriteString(string.Empty);

                this.GetWrittenData().Should().Equal(1, 2, (byte)'=');
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.formatter.WriteBeginProperty("Aa");
                this.formatter.Writer.WriteString(string.Empty);

                this.GetWrittenData().Should().Equal((byte)'A', (byte)'a', (byte)'=');
            }
        }

        public sealed class WriteElementSeparator : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldIncreaseTheIndex()
            {
                this.formatter.WriteBeginArray(typeof(int[]), 2);
                this.formatter.WriteElementSeparator();
                this.formatter.Writer.WriteString(string.Empty);

                byte[] written = this.GetWrittenData();
                written.Should().Equal(new[] { (byte)'1', (byte)'=' });
            }
        }

        public sealed class WriteEndArray : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldRemoveTheIndex()
            {
                this.formatter.WriteBeginArray(typeof(int[]), 1);
                this.formatter.WriteEndArray();
                this.formatter.Writer.WriteString(string.Empty);

                this.GetWrittenData().Should().BeEmpty();
            }
        }

        public sealed class WriteEndClass : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.formatter.Invoking(x => x.WriteEndClass())
                    .Should().NotThrow();
            }
        }

        public sealed class WriteEndPrimitive : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.formatter.Invoking(x => x.WriteEndPrimitive())
                    .Should().NotThrow();
            }
        }

        public sealed class WriteEndProperty : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldRemoveTheMetadata()
            {
                this.formatter.WriteBeginProperty(new byte[] { 1, 2 });
                this.formatter.WriteEndProperty();
                this.formatter.Writer.WriteString(string.Empty);

                this.GetWrittenData().Should().BeEmpty();
            }
        }
    }
}
