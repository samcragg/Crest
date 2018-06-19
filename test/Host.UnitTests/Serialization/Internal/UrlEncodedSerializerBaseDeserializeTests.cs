namespace Host.UnitTests.Serialization.Internal
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using Xunit;

    public class UrlEncodedSerializerBaseDeserializeTests
    {
        private readonly Lazy<FakeUrlSerializerBase> serializer;
        private readonly Stream stream;

        private UrlEncodedSerializerBaseDeserializeTests()
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

        public sealed class BeginRead : UrlEncodedSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.Serializer.Invoking(x => x.BeginRead(null))
                    .Should().NotThrow();
            }
        }

        public sealed class Constructor : UrlEncodedSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldUseTheSameStreamReader()
            {
                var copy = new FakeUrlSerializerBase(this.Serializer);

                copy.Reader.Should().BeSameAs(this.Serializer.Reader);
            }
        }

        public sealed class EndRead : UrlEncodedSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.Serializer.Invoking(x => x.EndRead())
                    .Should().NotThrow();
            }
        }

        public sealed class ReadBeginArray : UrlEncodedSerializerBaseDeserializeTests
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

        public sealed class ReadBeginClass : UrlEncodedSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldResetThePropertiesThatHaveBeenRead()
            {
                this.SetStreamTo("A=x&B=y");
                this.Serializer.ReadBeginProperty();
                this.Serializer.ReadEndProperty();

                this.Serializer.ReadBeginClass(null);

                // Read begin property doesn't skip if it's the first property
                // being read
                this.Serializer.ReadBeginProperty()
                    .Should().Be("A");
            }
        }

        public sealed class ReadBeginProperty : UrlEncodedSerializerBaseDeserializeTests
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

        public sealed class ReadElementSeparator : UrlEncodedSerializerBaseDeserializeTests
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

        public sealed class ReadEndArray : UrlEncodedSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldNotThrowAnyException()
            {
                this.Serializer.Invoking(x => x.ReadEndArray())
                    .Should().NotThrow();
            }
        }

        public sealed class ReadEndClass : UrlEncodedSerializerBaseDeserializeTests
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
