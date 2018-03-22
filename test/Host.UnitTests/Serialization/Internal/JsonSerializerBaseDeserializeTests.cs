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

        public sealed class ReadBeginClass : JsonSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldConsumeTheStartToken()
            {
                this.SetStreamTo("{1");

                this.Serializer.ReadBeginClass(null);
                int result = this.Serializer.Reader.ReadInt32();

                result.Should().Be(1);
            }

            [Fact]
            public void ShouldThrowIfNotAtTheStartOfAClass()
            {
                this.SetStreamTo("123");

                Action action = () => this.Serializer.ReadBeginClass(null);

                action.Should().Throw<FormatException>();
            }
        }

        public sealed class ReadBeginProperty : JsonSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldReturnNullIfNotAProperty()
            {
                this.SetStreamTo("123");

                string result = this.Serializer.ReadBeginProperty();

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnThePropertyName()
            {
                this.SetStreamTo("\"property\":123");

                string result = this.Serializer.ReadBeginProperty();

                result.Should().Be("property");
            }

            [Fact]
            public void ShouldThrowMissingTheValueSeparator()
            {
                this.SetStreamTo("\"property\" 123");

                Action action = () => this.Serializer.ReadBeginProperty();

                action.Should().Throw<FormatException>()
                      .WithMessage("*:*");
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

        public sealed class ReadEndClass : JsonSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldConsumeTheEndToken()
            {
                this.SetStreamTo("}1");

                this.Serializer.ReadEndClass();
                int result = this.Serializer.Reader.ReadInt32();

                result.Should().Be(1);
            }

            [Fact]
            public void ShouldThrowIfNotAtTheEndOfAClass()
            {
                this.SetStreamTo("1");

                Action action = () => this.Serializer.ReadEndClass();

                action.Should().Throw<FormatException>();
            }
        }

        public sealed class ReadEndProperty : JsonSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldConsumePropertySeparators()
            {
                this.SetStreamTo(",1");

                this.Serializer.ReadEndProperty();
                int result = this.Serializer.Reader.ReadInt32();

                result.Should().Be(1);
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
