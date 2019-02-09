namespace Host.UnitTests.Serialization.Xml
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization.Internal;
    using Crest.Host.Serialization.Xml;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class XmlFormatterDeserializeTests
    {
        private readonly Lazy<XmlFormatter> formatter;
        private readonly Stream stream;

        private XmlFormatterDeserializeTests()
        {
            this.stream = new MemoryStream();
            this.formatter = new Lazy<XmlFormatter>(
                () => new XmlFormatter(this.stream, SerializationMode.Deserialize));
        }

        private XmlFormatter Formatter => this.formatter.Value;

        private void SetStreamTo(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            this.stream.Write(bytes, 0, bytes.Length);
            this.stream.Position = 0;
        }

        public sealed class Dispose : XmlFormatterDeserializeTests
        {
            [Fact]
            public void ShouldDisposeTheStream()
            {
                Stream mockStream = Substitute.For<Stream>();
                var fakeSerializer = new XmlFormatter(mockStream, SerializationMode.Deserialize);

                fakeSerializer.Dispose();

                ((IDisposable)mockStream).Received().Dispose();
            }
        }

        public sealed class ReadBeginArray : XmlFormatterDeserializeTests
        {
            [Fact]
            public void ShouldReadRootArrayElements()
            {
                this.SetStreamTo("<ArrayOfint><int /><ArrayOfint>");

                bool result = this.Formatter.ReadBeginArray(typeof(int));

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnFalseForEmptyArrayElements()
            {
                this.SetStreamTo("<ArrayOfint />");

                bool result = this.Formatter.ReadBeginArray(typeof(int));

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueForNonEmptyElements()
            {
                this.SetStreamTo("<Property><int /></Property>");

                this.Formatter.ReadBeginProperty();
                bool result = this.Formatter.ReadBeginArray(typeof(int));

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldThrowIfTheElementNameIsIncorrect()
            {
                this.SetStreamTo("<ArrayOfstring><int /></ArrayOfstring>");

                Action action = () => this.Formatter.ReadBeginArray(typeof(int));

                action.Should().Throw<FormatException>()
                      .WithMessage("*int*");
            }
        }

        public sealed class ReadBeginClass : XmlFormatterDeserializeTests
        {
            [Fact]
            public void ShouldNotReadTheElementForNestedClasses()
            {
                this.SetStreamTo("<Property><NestedProperty /></Property>");

                this.Formatter.ReadBeginProperty();
                this.Formatter.ReadBeginClass((object)"Class");
                string property = this.Formatter.ReadBeginProperty();

                property.Should().Be("NestedProperty");
            }

            [Fact]
            public void ShouldReadTheRootElement()
            {
                this.SetStreamTo("<Class>1</Class>");

                this.Formatter.ReadBeginClass((object)"Class");
                int content = this.Formatter.Reader.ReadInt32();

                content.Should().Be(1);
            }
        }

        public sealed class ReadBeginPrimitive : XmlFormatterDeserializeTests
        {
            [Fact]
            public void ShouldIgnoreTheCaseOfTheElement()
            {
                this.SetStreamTo("<className>1</className>");

                this.Formatter.ReadBeginPrimitive("ClassName");
                int content = this.Formatter.Reader.ReadInt32();

                content.Should().Be(1);
            }

            [Fact]
            public void ShouldThrowIfNotTheExpectedStartElement()
            {
                this.SetStreamTo("<element />");

                Action action = () => this.Formatter.ReadBeginPrimitive("className");

                action.Should().Throw<FormatException>()
                      .WithMessage("*className*");
            }
        }

        public sealed class ReadBeginProperty : XmlFormatterDeserializeTests
        {
            [Fact]
            public void ShouldReturnNullIfThereIsNoElement()
            {
                this.SetStreamTo("<Class />");

                this.Formatter.ReadBeginClass("Class");
                string property = this.Formatter.ReadBeginProperty();

                property.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheNameOfTheElement()
            {
                this.SetStreamTo("<Property>1</Property>");

                string property = this.Formatter.ReadBeginProperty();

                property.Should().Be("Property");
            }
        }

        public sealed class ReadElementSeparator : XmlFormatterDeserializeTests
        {
            [Fact]
            public void ShouldReturnFalseIfThereAreNoMoreElements()
            {
                this.SetStreamTo("<ArrayOfint><int /></ArrayOfint>");

                this.Formatter.ReadBeginArray(typeof(int));
                this.Formatter.ReadBeginPrimitive("int");
                this.Formatter.ReadEndPrimitive();
                bool result = this.Formatter.ReadElementSeparator();

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfThereAreMoreElements()
            {
                this.SetStreamTo("<ArrayOfint><int /><int /></ArrayOfint>");

                this.Formatter.ReadBeginArray(typeof(int));
                bool result = this.Formatter.ReadElementSeparator();

                result.Should().BeTrue();
            }
        }

        public sealed class ReadEndArray : XmlFormatterDeserializeTests
        {
            [Fact]
            public void ShouldReadTheEndElement()
            {
                this.SetStreamTo("<ArrayOfint><int /></ArrayOfint> 1");

                this.Formatter.ReadBeginArray(typeof(int));
                this.Formatter.ReadBeginPrimitive("int");
                this.Formatter.ReadEndPrimitive();
                this.Formatter.ReadEndArray();
                int content = this.Formatter.Reader.ReadInt32();

                content.Should().Be(1);
            }
        }

        public sealed class ReadEndClass : XmlFormatterDeserializeTests
        {
            [Fact]
            public void ShouldReadTheEndElement()
            {
                this.SetStreamTo("<Class></Class> 1");

                this.Formatter.ReadBeginClass("Class");
                this.Formatter.ReadEndClass();
                int content = this.Formatter.Reader.ReadInt32();

                content.Should().Be(1);
            }
        }

        public sealed class ReadEndPrimitive : XmlFormatterDeserializeTests
        {
            [Fact]
            public void ShouldMovePastTheEndElement()
            {
                this.SetStreamTo("<first></first><second>2</second>");

                this.Formatter.ReadBeginPrimitive("first");
                this.Formatter.ReadEndPrimitive();
                this.Formatter.ReadBeginPrimitive("second");
                int content = this.Formatter.Reader.ReadInt32();

                content.Should().Be(2);
            }
        }

        public sealed class ReadEndProperty : XmlFormatterDeserializeTests
        {
            [Fact]
            public void ShouldReadTheEndElement()
            {
                this.SetStreamTo("<Property></Property> 1");

                this.Formatter.ReadBeginProperty();
                this.Formatter.ReadEndProperty();
                int content = this.Formatter.Reader.ReadInt32();

                content.Should().Be(1);
            }
        }
    }
}
