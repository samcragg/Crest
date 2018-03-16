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

        public sealed class Constructor : UrlEncodedSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldUseTheSameStreamReader()
            {
                var copy = new FakeUrlSerializerBase(this.Serializer);

                copy.Reader.Should().BeSameAs(this.Serializer.Reader);
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
