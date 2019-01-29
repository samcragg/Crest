namespace Host.UnitTests.Serialization.UrlEncoded
{
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class UrlEncodedFormatterSerializeTests
    {
        private readonly UrlEncodedSerializerBase serializer;
        private readonly MemoryStream stream = new MemoryStream();

        protected UrlEncodedFormatterSerializeTests()
        {
            this.serializer = new FakeUrlEncodedSerializerBase(this.stream);
        }

        protected byte[] GetWrittenData()
        {
            this.serializer.Flush();
            return this.stream.ToArray();
        }

        public sealed class BeginWrite : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.serializer.Invoking(x => x.BeginWrite(null))
                    .Should().NotThrow();
            }
        }

        public sealed class Constructor : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldCreateAStreamWriter()
            {
                var instance = new FakeUrlEncodedSerializerBase(Stream.Null);

                instance.Writer.Should().NotBeNull();
            }

            [Fact]
            public void ShouldSetTheWriter()
            {
                var parent = new FakeUrlEncodedSerializerBase(Stream.Null);

                var instance = new FakeUrlEncodedSerializerBase(parent);

                instance.Writer.Should().BeSameAs(parent.Writer);
            }
        }

        public sealed class EndWrite : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.serializer.Invoking(x => x.EndWrite())
                    .Should().NotThrow();
            }
        }

        public sealed class Flush : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldFlushTheBuffers()
            {
                Stream stream = Substitute.For<Stream>();
                UrlEncodedSerializerBase serializer = new FakeUrlEncodedSerializerBase(stream);

                serializer.WriteBeginProperty(new byte[12]);
                stream.DidNotReceiveWithAnyArgs().Write(null, 0, 0);

                serializer.Flush();
                stream.ReceivedWithAnyArgs().Write(null, 0, 0);
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
                byte[] result = UrlEncodedSerializerBase.GetMetadata(
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

        public sealed class OutputEnumNames : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldReturnTrue()
            {
                // Try to match XML, which uses the names
                UrlEncodedSerializerBase.OutputEnumNames.Should().BeTrue();
            }
        }

        public sealed class WriteBeginArray : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteTheZeroIndex()
            {
                this.serializer.WriteBeginArray(typeof(int[]), 1);
                this.serializer.Writer.WriteString(string.Empty);

                this.GetWrittenData().Should().Equal((byte)'0', (byte)'=');
            }
        }

        public sealed class WriteBeginClass : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyExceptionForByteArrays()
            {
                this.serializer.Invoking(x => x.WriteBeginClass((byte[])null))
                    .Should().NotThrow();
            }

            [Fact]
            public void ShouldNotThrowAnyExceptionForStrings()
            {
                this.serializer.Invoking(x => x.WriteBeginClass((string)null))
                    .Should().NotThrow();
            }
        }

        public sealed class WriteBeginProperty : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteTheMetadata()
            {
                this.serializer.WriteBeginProperty(new byte[] { 1, 2 });
                this.serializer.Writer.WriteString(string.Empty);

                this.GetWrittenData().Should().Equal(1, 2, (byte)'=');
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.serializer.WriteBeginProperty("Aa");
                this.serializer.Writer.WriteString(string.Empty);

                this.GetWrittenData().Should().Equal((byte)'A', (byte)'a', (byte)'=');
            }
        }

        public sealed class WriteElementSeparator : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldIncreaseTheIndex()
            {
                this.serializer.WriteBeginArray(typeof(int[]), 2);
                this.serializer.WriteElementSeparator();
                this.serializer.Writer.WriteString(string.Empty);

                byte[] written = this.GetWrittenData();
                written.Should().Equal(new[] { (byte)'1', (byte)'=' });
            }
        }

        public sealed class WriteEndArray : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldRemoveTheIndex()
            {
                this.serializer.WriteBeginArray(typeof(int[]), 1);
                this.serializer.WriteEndArray();
                this.serializer.Writer.WriteString(string.Empty);

                this.GetWrittenData().Should().BeEmpty();
            }
        }

        public sealed class WriteEndClass : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.serializer.Invoking(x => x.WriteEndClass())
                    .Should().NotThrow();
            }
        }

        public sealed class WriteEndProperty : UrlEncodedFormatterSerializeTests
        {
            [Fact]
            public void ShouldRemoveTheMetadata()
            {
                this.serializer.WriteBeginProperty(new byte[] { 1, 2 });
                this.serializer.WriteEndProperty();
                this.serializer.Writer.WriteString(string.Empty);

                this.GetWrittenData().Should().BeEmpty();
            }
        }

        private sealed class FakeUrlEncodedSerializerBase : UrlEncodedSerializerBase
        {
            public FakeUrlEncodedSerializerBase(Stream stream) : base(stream, SerializationMode.Serialize)
            {
            }

            public FakeUrlEncodedSerializerBase(UrlEncodedSerializerBase parent) : base(parent)
            {
            }
        }
    }
}
