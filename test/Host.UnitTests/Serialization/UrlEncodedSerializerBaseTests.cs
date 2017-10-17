namespace Host.UnitTests.Serialization
{
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class UrlEncodedSerializerBaseTests
    {
        private readonly UrlEncodedSerializerBase serializer;
        private readonly MemoryStream stream = new MemoryStream();

        protected UrlEncodedSerializerBaseTests()
        {
            this.serializer = new FakeUrlEncodedSerializerBase(this.stream);
        }

        protected byte[] GetWrittenData()
        {
            this.serializer.Flush();
            return this.stream.ToArray();
        }

        public sealed class Constructor : UrlEncodedSerializerBaseTests
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

        public sealed class Flush : UrlEncodedSerializerBaseTests
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

        public sealed class GetMetadata : UrlEncodedSerializerBaseTests
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

        public sealed class OutputEnumNames : UrlEncodedSerializerBaseTests
        {
            [Fact]
            public void ShouldReturnTrue()
            {
                // Try to match XML, which uses the names
                UrlEncodedSerializerBase.OutputEnumNames.Should().BeTrue();
            }
        }

        public sealed class WriteBeginArray : UrlEncodedSerializerBaseTests
        {
            [Fact]
            public void ShouldWriteTheZeroIndex()
            {
                this.serializer.WriteBeginArray(typeof(int[]), 1);
                this.serializer.Writer.WriteString(string.Empty);

                this.GetWrittenData().Should().Equal((byte)'0', (byte)'=');
            }
        }

        public sealed class WriteBeginProperty : UrlEncodedSerializerBaseTests
        {
            [Fact]
            public void ShouldWriteTheMetadata()
            {
                this.serializer.WriteBeginProperty(new byte[] { 1, 2 });
                this.serializer.Writer.WriteString(string.Empty);

                this.GetWrittenData().Should().Equal(1, 2, (byte)'=');
            }
        }

        public sealed class WriteElementSeparator : UrlEncodedSerializerBaseTests
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

        private sealed class FakeUrlEncodedSerializerBase : UrlEncodedSerializerBase
        {
            public FakeUrlEncodedSerializerBase(Stream stream) : base(stream)
            {
            }

            public FakeUrlEncodedSerializerBase(UrlEncodedSerializerBase parent) : base(parent)
            {
            }
        }
    }
}
