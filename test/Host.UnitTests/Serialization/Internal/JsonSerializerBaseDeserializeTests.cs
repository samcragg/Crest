namespace Host.UnitTests.Serialization.Internal
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class JsonSerializerBaseDeserializeTests
    {
        private readonly Lazy<FakeJsonSerializerBase> serializer;
        private readonly Stream stream;

        private JsonSerializerBaseDeserializeTests()
        {
            this.stream = new MemoryStream();
            this.serializer = new Lazy<FakeJsonSerializerBase>(
                () => new FakeJsonSerializerBase(this.stream));
        }

        private JsonSerializerBase Serializer => this.serializer.Value;

        private void SetStreamTo(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            this.stream.Write(bytes, 0, bytes.Length);
            this.stream.Position = 0;
        }

        public sealed class Constructor : JsonSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldUseTheSameStreamReader()
            {
                var copy = new FakeJsonSerializerBase(this.Serializer);

                copy.Reader.Should().BeSameAs(this.Serializer.Reader);
            }
        }

        public sealed class Dispose : JsonSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldDisposeTheStream()
            {
                Stream mockStream = Substitute.For<Stream>();
                var fakeSerializer = new FakeJsonSerializerBase(mockStream);

                fakeSerializer.Dispose();

                mockStream.Received().Dispose();
            }
        }

        public sealed class ReadBeginArray : JsonSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldReturnFalseForEmptyArrays()
            {
                this.SetStreamTo("[ ]");

                bool result = this.Serializer.ReadBeginArray(typeof(int));

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseIfNotAtTheStartOfAnArray()
            {
                this.SetStreamTo("123");

                bool result = this.Serializer.ReadBeginArray(typeof(int));

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfAtTheStartOfAnArray()
            {
                this.SetStreamTo("[1]");

                bool result = this.Serializer.ReadBeginArray(typeof(int));

                result.Should().BeTrue();
            }
        }

        public sealed class ReadElementSeparator : JsonSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldReturnFalseIfThereIsNoSeparator()
            {
                this.SetStreamTo("123");

                bool result = this.Serializer.ReadElementSeparator();

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfThereIsASeparator()
            {
                this.SetStreamTo(",");

                bool result = this.Serializer.ReadElementSeparator();

                result.Should().BeTrue();
            }
        }

        public sealed class ReadEndArray : JsonSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldConsumeTheEndToken()
            {
                this.SetStreamTo("]1");

                this.Serializer.ReadEndArray();
                int result = this.Serializer.Reader.ReadInt32();

                result.Should().Be(1);
            }

            [Fact]
            public void ShouldThrowIfNotAtTheEndOfAnArray()
            {
                this.SetStreamTo("1");

                Action action = () => this.Serializer.ReadEndArray();

                action.Should().Throw<FormatException>();
            }
        }

        private class FakeJsonSerializerBase : JsonSerializerBase
        {
            internal FakeJsonSerializerBase(Stream stream)
                : base(stream, SerializationMode.Deserialize)
            {
            }

            internal FakeJsonSerializerBase(JsonSerializerBase parent)
                : base(parent)
            {
            }
        }
    }
}
