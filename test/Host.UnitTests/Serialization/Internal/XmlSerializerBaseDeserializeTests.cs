namespace Host.UnitTests.Serialization.Internal
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class XmlSerializerBaseDeserializeTests
    {
        private readonly Lazy<FakeXmlSerializerBase> serializer;
        private readonly Stream stream;

        private XmlSerializerBaseDeserializeTests()
        {
            this.stream = new MemoryStream();
            this.serializer = new Lazy<FakeXmlSerializerBase>(
                () => new FakeXmlSerializerBase(this.stream));
        }

        private XmlSerializerBase Serializer => this.serializer.Value;

        private void SetStreamTo(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            this.stream.Write(bytes, 0, bytes.Length);
            this.stream.Position = 0;
        }

        public sealed class BeginRead : XmlSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldIgnoreTheCaseOfTheElement()
            {
                this.SetStreamTo("<className>1</className>");

                this.Serializer.BeginRead("ClassName");
                int content = this.Serializer.Reader.ReadInt32();

                content.Should().Be(1);
            }

            [Fact]
            public void ShouldThrowIfNotTheExpectedStartElement()
            {
                this.SetStreamTo("<element />");

                Action action = () => this.Serializer.BeginRead("className");

                action.Should().Throw<FormatException>()
                      .WithMessage("*className*");
            }
        }

        public sealed class Constructor : XmlSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldUseTheSameStreamReader()
            {
                var copy = new FakeXmlSerializerBase(this.Serializer);

                copy.Reader.Should().BeSameAs(this.Serializer.Reader);
            }
        }

        public sealed class Dispose : XmlSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldDisposeTheStream()
            {
                Stream mockStream = Substitute.For<Stream>();
                var fakeSerializer = new FakeXmlSerializerBase(mockStream);

                fakeSerializer.Dispose();

                mockStream.Received().Dispose();
            }
        }

        public sealed class ReadBeginArray : XmlSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldReadRootArrayElements()
            {
                this.SetStreamTo("<ArrayOfint><int /><ArrayOfint>");

                bool result = this.Serializer.ReadBeginArray(typeof(int));

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnFalseForEmptyArrayElements()
            {
                this.SetStreamTo("<ArrayOfint />");

                bool result = this.Serializer.ReadBeginArray(typeof(int));

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueForNonEmptyElements()
            {
                this.SetStreamTo("<Property><int /></Property>");

                this.Serializer.ReadBeginProperty();
                bool result = this.Serializer.ReadBeginArray(typeof(int));

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldThrowIfTheElementNameIsIncorrect()
            {
                this.SetStreamTo("<ArrayOfint><string /></ArrayOfint>");

                Action action = () => this.Serializer.ReadBeginArray(typeof(int));

                action.Should().Throw<FormatException>()
                      .WithMessage("*int*");
            }
        }

        public sealed class ReadBeginClass : XmlSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldNotReadTheElementForNestedClasses()
            {
                this.SetStreamTo("<Property><NestedProperty /></Property>");

                this.Serializer.ReadBeginProperty();
                this.Serializer.ReadBeginClass("Class");
                string property = this.Serializer.ReadBeginProperty();

                property.Should().Be("NestedProperty");
            }

            [Fact]
            public void ShouldReadTheRootElement()
            {
                this.SetStreamTo("<Class>1</Class>");

                this.Serializer.ReadBeginClass("Class");
                int content = this.Serializer.Reader.ReadInt32();

                content.Should().Be(1);
            }
        }

        public sealed class ReadBeginProperty : XmlSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldReturnNullIfThereIsNoElement()
            {
                this.SetStreamTo("<Class />");

                this.Serializer.ReadBeginClass("Class");
                string property = this.Serializer.ReadBeginProperty();

                property.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheNameOfTheElement()
            {
                this.SetStreamTo("<Property>1</Property>");

                string property = this.Serializer.ReadBeginProperty();

                property.Should().Be("Property");
            }
        }

        public sealed class ReadElementSeparator : XmlSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldCheckTheNameOfTheElement()
            {
                this.SetStreamTo("<ArrayOfint><int /><string /></ArrayOfint>");

                this.Serializer.ReadBeginArray(typeof(int));
                Action action = () => this.Serializer.ReadElementSeparator();

                action.Should().Throw<FormatException>()
                      .WithMessage("*int*");
            }

            [Fact]
            public void ShouldReturnFalseIfThereAreNoMoreElements()
            {
                this.SetStreamTo("<ArrayOfint><int /></ArrayOfint>");

                this.Serializer.ReadBeginArray(typeof(int));
                bool result = this.Serializer.ReadElementSeparator();

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfThereAreMoreElements()
            {
                this.SetStreamTo("<ArrayOfint><int /><int /></ArrayOfint>");

                this.Serializer.ReadBeginArray(typeof(int));
                bool result = this.Serializer.ReadElementSeparator();

                result.Should().BeTrue();
            }
        }

        public sealed class ReadEndArray : XmlSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldReadTheEndElement()
            {
                this.SetStreamTo("<ArrayOfint><int /></ArrayOfint> 1");

                this.Serializer.ReadBeginArray(typeof(int));
                this.Serializer.ReadElementSeparator();
                this.Serializer.ReadEndArray();
                int content = this.Serializer.Reader.ReadInt32();

                content.Should().Be(1);
            }
        }

        public sealed class ReadEndClass : XmlSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldReadTheEndElement()
            {
                this.SetStreamTo("<Class></Class> 1");

                this.Serializer.ReadBeginClass("Class");
                this.Serializer.ReadEndClass();
                int content = this.Serializer.Reader.ReadInt32();

                content.Should().Be(1);
            }
        }

        public sealed class ReadEndProperty : XmlSerializerBaseDeserializeTests
        {
            [Fact]
            public void ShouldReadTheEndElement()
            {
                this.SetStreamTo("<Property></Property> 1");

                this.Serializer.ReadBeginProperty();
                this.Serializer.ReadEndProperty();
                int content = this.Serializer.Reader.ReadInt32();

                content.Should().Be(1);
            }
        }

        private class FakeXmlSerializerBase : XmlSerializerBase
        {
            internal FakeXmlSerializerBase(Stream stream)
                : base(stream, SerializationMode.Deserialize)
            {
            }

            internal FakeXmlSerializerBase(XmlSerializerBase parent)
                : base(parent)
            {
            }
        }
    }
}
