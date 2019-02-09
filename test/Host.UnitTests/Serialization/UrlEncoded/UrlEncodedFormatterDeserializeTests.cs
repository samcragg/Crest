namespace Host.UnitTests.Serialization.UrlEncoded
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization.Internal;
    using Crest.Host.Serialization.UrlEncoded;
    using FluentAssertions;
    using Xunit;

    public class UrlEncodedFormatterDeserializeTests
    {
        private readonly Lazy<UrlEncodedFormatter> formatter;
        private readonly Stream stream;

        private UrlEncodedFormatterDeserializeTests()
        {
            this.stream = new MemoryStream();
            this.formatter = new Lazy<UrlEncodedFormatter>(
                () => new UrlEncodedFormatter(this.stream, SerializationMode.Deserialize));
        }

        private UrlEncodedFormatter Formatter => this.formatter.Value;

        private void SetStreamTo(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            this.stream.Write(bytes, 0, bytes.Length);
            this.stream.Position = 0;
        }

        public sealed class ReadBeginArray : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldReturnFalseForNonIntegerParts()
            {
                this.SetStreamTo("value=x");

                bool result = this.Formatter.ReadBeginArray(null);

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueAtTheStartOfAnIntegerPart()
            {
                this.SetStreamTo("1=x");

                bool result = this.Formatter.ReadBeginArray(null);

                result.Should().BeTrue();
            }
        }

        public sealed class ReadBeginClass : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldResetThePropertiesThatHaveBeenRead()
            {
                this.SetStreamTo("A=x&B=y");
                this.Formatter.ReadBeginProperty();
                this.Formatter.ReadEndProperty();

                this.Formatter.ReadBeginClass((byte[])null);

                // Read begin property doesn't skip if it's the first property
                // being read
                this.Formatter.ReadBeginProperty()
                    .Should().Be("A");
            }

            [Fact]
            public void ShouldResetThePropertiesThatHaveBeenReadForStrings()
            {
                this.SetStreamTo("A=x&B=y");
                this.Formatter.ReadBeginProperty();
                this.Formatter.ReadEndProperty();

                this.Formatter.ReadBeginClass((string)null);

                this.Formatter.ReadBeginProperty()
                    .Should().Be("A");
            }
        }

        public sealed class ReadBeginPrimitive : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.Formatter.Invoking(x => x.ReadBeginPrimitive(null))
                    .Should().NotThrow();
            }
        }

        public sealed class ReadBeginProperty : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldMoveToTheNextProperty()
            {
                this.SetStreamTo("A=x&B=y");
                this.Formatter.ReadBeginProperty();
                this.Formatter.ReadEndProperty();

                string result = this.Formatter.ReadBeginProperty();

                // Read begin property doesn't skip if it's the first property
                // being read
                result.Should().Be("B");
            }

            [Fact]
            public void ShouldReturnNullWhenThereAreNoMoreProperties()
            {
                this.SetStreamTo("A=x");
                this.Formatter.ReadBeginProperty();
                this.Formatter.ReadEndProperty();

                string result = this.Formatter.ReadBeginProperty();

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheKeyPart()
            {
                this.SetStreamTo("0.A=x");

                this.Formatter.ReadBeginArray(null);
                string result = this.Formatter.ReadBeginProperty();

                result.Should().Be("A");
            }
        }

        public sealed class ReadElementSeparator : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldReturnFalseAtTheEndOfTheArray()
            {
                this.SetStreamTo("1=x");

                this.Formatter.ReadBeginArray(null);
                bool result = this.Formatter.ReadElementSeparator();

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfThereAreMoreArrayParts()
            {
                this.SetStreamTo("1=x&2=y");

                this.Formatter.ReadBeginArray(null);
                bool result = this.Formatter.ReadElementSeparator();

                result.Should().BeTrue();
            }
        }

        public sealed class ReadEndArray : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.Formatter.Invoking(x => x.ReadEndArray())
                    .Should().NotThrow();
            }
        }

        public sealed class ReadEndClass : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.Formatter.Invoking(x => x.ReadEndClass())
                    .Should().NotThrow();
            }
        }

        public sealed class ReadEndPrimitive : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.Formatter.Invoking(x => x.ReadEndPrimitive())
                    .Should().NotThrow();
            }
        }

        public sealed class Reader : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldBeAbleToReadValues()
            {
                this.SetStreamTo("A=123");
                this.Formatter.ReadBeginProperty();

                int result = this.Formatter.Reader.ReadInt32();

                result.Should().Be(123);
            }
        }
    }
}
