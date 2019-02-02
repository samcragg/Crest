namespace Host.UnitTests.Serialization.Json
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization.Internal;
    using Crest.Host.Serialization.Json;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class JsonFormatterDeserializeTests
    {
        private readonly Lazy<JsonFormatter> formatter;
        private readonly Stream stream;

        private JsonFormatterDeserializeTests()
        {
            this.stream = new MemoryStream();
            this.formatter = new Lazy<JsonFormatter>(
                () => new JsonFormatter(this.stream, SerializationMode.Deserialize));
        }

        private JsonFormatter Formatter => this.formatter.Value;

        private void SetStreamTo(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            this.stream.Write(bytes, 0, bytes.Length);
            this.stream.Position = 0;
        }

        public sealed class Dispose : JsonFormatterDeserializeTests
        {
            [Fact]
            public void ShouldDisposeTheStream()
            {
                Stream mockStream = Substitute.For<Stream>();
                var fakeSerializer = new JsonFormatter(mockStream, SerializationMode.Deserialize);

                fakeSerializer.Dispose();

                ((IDisposable)mockStream).Received().Dispose();
            }
        }

        public sealed class ReadBeginArray : JsonFormatterDeserializeTests
        {
            [Fact]
            public void ShouldReturnFalseForEmptyArrays()
            {
                this.SetStreamTo("[ ]");

                bool result = this.Formatter.ReadBeginArray(typeof(int));

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseIfNotAtTheStartOfAnArray()
            {
                this.SetStreamTo("123");

                bool result = this.Formatter.ReadBeginArray(typeof(int));

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfAtTheStartOfAnArray()
            {
                this.SetStreamTo("[1]");

                bool result = this.Formatter.ReadBeginArray(typeof(int));

                result.Should().BeTrue();
            }
        }

        public sealed class ReadBeginClass : JsonFormatterDeserializeTests
        {
            [Fact]
            public void ShouldConsumeTheStartTokenForByteArrays()
            {
                this.SetStreamTo("{1");

                this.Formatter.ReadBeginClass((byte[])null);
                int result = this.Formatter.Reader.ReadInt32();

                result.Should().Be(1);
            }

            [Fact]
            public void ShouldConsumeTheStartTokenStrings()
            {
                this.SetStreamTo("{1");

                this.Formatter.ReadBeginClass((string)null);
                int result = this.Formatter.Reader.ReadInt32();

                result.Should().Be(1);
            }

            [Fact]
            public void ShouldThrowIfNotAtTheStartOfAClass()
            {
                this.SetStreamTo("123");

                Action action = () => this.Formatter.ReadBeginClass((byte[])null);

                action.Should().Throw<FormatException>();
            }
        }

        public sealed class ReadBeginPrimitive : JsonFormatterDeserializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.Formatter.Invoking(x => x.ReadBeginPrimitive(null))
                    .Should().NotThrow();
            }
        }

        public sealed class ReadBeginProperty : JsonFormatterDeserializeTests
        {
            [Fact]
            public void ShouldReturnNullIfNotAProperty()
            {
                this.SetStreamTo("123");

                string result = this.Formatter.ReadBeginProperty();

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnThePropertyName()
            {
                this.SetStreamTo("\"property\":123");

                string result = this.Formatter.ReadBeginProperty();

                result.Should().Be("property");
            }

            [Fact]
            public void ShouldThrowMissingTheValueSeparator()
            {
                this.SetStreamTo("\"property\" 123");

                Action action = () => this.Formatter.ReadBeginProperty();

                action.Should().Throw<FormatException>()
                      .WithMessage("*:*");
            }
        }

        public sealed class ReadElementSeparator : JsonFormatterDeserializeTests
        {
            [Fact]
            public void ShouldReturnFalseIfThereIsNoSeparator()
            {
                this.SetStreamTo("123");

                bool result = this.Formatter.ReadElementSeparator();

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfThereIsASeparator()
            {
                this.SetStreamTo(",");

                bool result = this.Formatter.ReadElementSeparator();

                result.Should().BeTrue();
            }
        }

        public sealed class ReadEndArray : JsonFormatterDeserializeTests
        {
            [Fact]
            public void ShouldConsumeTheEndToken()
            {
                this.SetStreamTo("]1");

                this.Formatter.ReadEndArray();
                int result = this.Formatter.Reader.ReadInt32();

                result.Should().Be(1);
            }

            [Fact]
            public void ShouldThrowIfNotAtTheEndOfAnArray()
            {
                this.SetStreamTo("1");

                Action action = () => this.Formatter.ReadEndArray();

                action.Should().Throw<FormatException>();
            }
        }

        public sealed class ReadEndClass : JsonFormatterDeserializeTests
        {
            [Fact]
            public void ShouldConsumeTheEndToken()
            {
                this.SetStreamTo("}1");

                this.Formatter.ReadEndClass();
                int result = this.Formatter.Reader.ReadInt32();

                result.Should().Be(1);
            }

            [Fact]
            public void ShouldThrowIfNotAtTheEndOfAClass()
            {
                this.SetStreamTo("1");

                Action action = () => this.Formatter.ReadEndClass();

                action.Should().Throw<FormatException>();
            }
        }

        public sealed class ReadEndPrimitive : JsonFormatterDeserializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.Formatter.Invoking(x => x.ReadEndPrimitive())
                    .Should().NotThrow();
            }
        }

        public sealed class ReadEndProperty : JsonFormatterDeserializeTests
        {
            [Fact]
            public void ShouldConsumePropertySeparators()
            {
                this.SetStreamTo(",1");

                this.Formatter.ReadEndProperty();
                int result = this.Formatter.Reader.ReadInt32();

                result.Should().Be(1);
            }
        }
    }
}
