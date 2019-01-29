namespace Host.UnitTests.Serialization.UrlEncoded
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using Xunit;

    public class UrlEncodedFormatterDeserializeTests
    {
        private readonly Lazy<FakeUrlSerializerBase> serializer;
        private readonly Stream stream;

        private UrlEncodedFormatterDeserializeTests()
        {
            this.stream = new MemoryStream();
            this.serializer = new Lazy<FakeUrlSerializerBase>(
                () => new FakeUrlSerializerBase(this.stream));
        }

        private UrlEncodedSerializerBase Serializer => this.serializer.Value;

        private void SetStreamTo(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            this.stream.Write(bytes, 0, bytes.Length);
            this.stream.Position = 0;
        }

        public sealed class BeginRead : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.Serializer.Invoking(x => x.BeginRead(null))
                    .Should().NotThrow();
            }
        }

        public sealed class Constructor : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldUseTheSameStreamReader()
            {
                var copy = new FakeUrlSerializerBase(this.Serializer);

                copy.Reader.Should().BeSameAs(this.Serializer.Reader);
            }
        }

        public sealed class EndRead : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.Serializer.Invoking(x => x.EndRead())
                    .Should().NotThrow();
            }
        }

        public sealed class ReadBeginArray : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldReturnFalseForNonIntegerParts()
            {
                this.SetStreamTo("value=x");

                bool result = this.Serializer.ReadBeginArray(null);

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueAtTheStartOfAnIntegerPart()
            {
                this.SetStreamTo("1=x");

                bool result = this.Serializer.ReadBeginArray(null);

                result.Should().BeTrue();
            }
        }

        public sealed class ReadBeginClass : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldResetThePropertiesThatHaveBeenRead()
            {
                this.SetStreamTo("A=x&B=y");
                this.Serializer.ReadBeginProperty();
                this.Serializer.ReadEndProperty();

                this.Serializer.ReadBeginClass((byte[])null);

                // Read begin property doesn't skip if it's the first property
                // being read
                this.Serializer.ReadBeginProperty()
                    .Should().Be("A");
            }

            [Fact]
            public void ShouldResetThePropertiesThatHaveBeenReadForStrings()
            {
                this.SetStreamTo("A=x&B=y");
                this.Serializer.ReadBeginProperty();
                this.Serializer.ReadEndProperty();

                this.Serializer.ReadBeginClass((string)null);

                this.Serializer.ReadBeginProperty()
                    .Should().Be("A");
            }
        }

        public sealed class ReadBeginProperty : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldMoveToTheNextProperty()
            {
                this.SetStreamTo("A=x&B=y");
                this.Serializer.ReadBeginProperty();
                this.Serializer.ReadEndProperty();

                string result = this.Serializer.ReadBeginProperty();

                // Read begin property doesn't skip if it's the first property
                // being read
                result.Should().Be("B");
            }

            [Fact]
            public void ShouldReturnNullWhenThereAreNoMoreProperties()
            {
                this.SetStreamTo("A=x");
                this.Serializer.ReadBeginProperty();
                this.Serializer.ReadEndProperty();

                string result = this.Serializer.ReadBeginProperty();

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheKeyPart()
            {
                this.SetStreamTo("0.A=x");

                this.Serializer.ReadBeginArray(null);
                string result = this.Serializer.ReadBeginProperty();

                result.Should().Be("A");
            }
        }

        public sealed class ReadElementSeparator : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldReturnFalseAtTheEndOfTheArray()
            {
                this.SetStreamTo("1=x");

                this.Serializer.ReadBeginArray(null);
                bool result = this.Serializer.ReadElementSeparator();

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfThereAreMoreArrayParts()
            {
                this.SetStreamTo("1=x&2=y");

                this.Serializer.ReadBeginArray(null);
                bool result = this.Serializer.ReadElementSeparator();

                result.Should().BeTrue();
            }
        }

        public sealed class ReadEndArray : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.Serializer.Invoking(x => x.ReadEndArray())
                    .Should().NotThrow();
            }
        }

        public sealed class ReadEndClass : UrlEncodedFormatterDeserializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.Serializer.Invoking(x => x.ReadEndClass())
                    .Should().NotThrow();
            }
        }

        private class FakeUrlSerializerBase : UrlEncodedSerializerBase
        {
            internal FakeUrlSerializerBase(Stream stream)
                : base(stream, SerializationMode.Deserialize)
            {
            }

            internal FakeUrlSerializerBase(UrlEncodedSerializerBase parent)
                : base(parent)
            {
            }
        }
    }
}
