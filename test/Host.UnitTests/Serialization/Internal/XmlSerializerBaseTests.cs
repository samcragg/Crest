namespace Host.UnitTests.Serialization.Internal
{
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class XmlSerializerBaseTests
    {
        private readonly XmlSerializerBase serializer;
        private readonly MemoryStream stream = new MemoryStream();

        private XmlSerializerBaseTests()
        {
            this.serializer = new FakeXmlSerializerBase(this.stream, SerializationMode.Serialize);
        }

        private XmlStreamWriter XmlStreamWriter => (XmlStreamWriter)this.serializer.Writer;

        private void ForceFullEndTag()
        {
            this.XmlStreamWriter.WriteString(string.Empty);
        }

        private string GetWrittenData()
        {
            this.serializer.Flush();
            string xml = Encoding.UTF8.GetString(this.stream.ToArray());

            // Strip the <?xml ?> part
            xml = xml.Substring(xml.IndexOf("?>") + 2);

            // Strip the namespace used for null values
            xml = xml.Replace(@" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""", string.Empty);

            // Ensure any opened element is fully formed, as XmlWriter will
            // wait for some content to be written before writing it in full
            // i.e. <element attribute="value"
            if (xml.LastIndexOf('<') > xml.LastIndexOf('>'))
            {
                xml += ">";
            }

            return xml;
        }

        public sealed class BeginWrite : XmlSerializerBaseTests
        {
            [Fact]
            public void ShouldChangeWriteTheStartElement()
            {
                this.serializer.BeginWrite("metadata");
                string written = this.GetWrittenData();

                written.Should().StartWith("<metadata>");
            }
        }

        public sealed class Constructor : XmlSerializerBaseTests
        {
            [Fact]
            public void ShouldCreateAStreamWriter()
            {
                var instance = new FakeXmlSerializerBase(Stream.Null, SerializationMode.Serialize);

                instance.Writer.Should().NotBeNull();
            }

            [Fact]
            public void ShouldSetTheWriter()
            {
                var parent = new FakeXmlSerializerBase(Stream.Null, SerializationMode.Serialize);

                var instance = new FakeXmlSerializerBase(parent);

                instance.Writer.Should().BeSameAs(parent.Writer);
            }
        }

        public sealed class EndWrite : XmlSerializerBaseTests
        {
            [Fact]
            public void ShouldCloseTheXmlElement()
            {
                this.serializer.BeginWrite("root");
                this.serializer.Writer.WriteInt32(1);

                this.serializer.EndWrite();
                string written = this.GetWrittenData();

                written.Should().EndWith("</root>");
            }
        }

        public sealed class Flush : XmlSerializerBaseTests
        {
            [Fact]
            public void ShouldFlushTheBuffers()
            {
                Stream stream = Substitute.For<Stream>();
                XmlSerializerBase serializer = new FakeXmlSerializerBase(stream, SerializationMode.Serialize);

                serializer.BeginWrite("root");
                stream.DidNotReceiveWithAnyArgs().Write(null, 0, 0);

                serializer.Flush();
                stream.ReceivedWithAnyArgs().Write(null, 0, 0);
            }
        }

        public sealed class GetMetadata : XmlSerializerBaseTests
        {
            [Fact]
            public void ShouldEscapeCharactersFromTheDisplayName()
            {
                string property = GetPropertyNameFromMetadata("HasSpecialCharacters");

                property.Should().Be("Element_x003C__x003E_Name");
            }

            [Fact]
            public void ShouldReturnTheNameOfTheProperty()
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
                return XmlSerializerBase.GetMetadata(
                    typeof(ExampleProperties).GetProperty(property));
            }

            private class ExampleProperties
            {
                [DisplayName("DisplayValue")]
                public int HasDisplayName { get; set; }

                [DisplayName("Element<>Name")]
                public int HasSpecialCharacters { get; set; }

                public int SimpleProperty { get; set; }
            }
        }

        public sealed class GetTypeMetadata : XmlSerializerBaseTests
        {
            [Fact]
            public void ShouldEscapeCharactersFromTheDisplayName()
            {
                // Unicode 0x363 is valid for C# but not for XML
                string result = XmlSerializerBase.GetTypeMetadata(
                    typeof(SpecialͣChar));

                result.Should().Be("Special_x0363_Char");
            }

            private class SpecialͣChar
            {
            }
        }

        public sealed class OutputEnumNames : XmlSerializerBaseTests
        {
            [Fact]
            public void ShouldReturnTrue()
            {
                // This matches the DataContractSerializer
                XmlSerializerBase.OutputEnumNames.Should().BeTrue();
            }
        }

        public sealed class WriteBeginArray : XmlSerializerBaseTests
        {
            [Fact]
            public void ShouldTransformPrimitiveElements()
            {
                this.XmlStreamWriter.WriteStartElement("root");

                this.serializer.WriteBeginArray(typeof(int), 1);
                string written = this.GetWrittenData();

                written.Should().StartWith("<root><int>");
            }

            [Fact]
            public void ShouldWriteTheRootElementIfNoneHasBeenWritten()
            {
                this.serializer.WriteBeginArray(typeof(EmptyClass), 1);
                string written = this.GetWrittenData();

                written.Should().StartWith("<ArrayOfEmptyClass>");
            }
        }

        public sealed class WriteBeginClass : XmlSerializerBaseTests
        {
            [Fact]
            public void ShouldNotWriteTheOpeningElementForNestedClasses()
            {
                this.XmlStreamWriter.WriteStartElement("root");

                this.serializer.WriteBeginClass(nameof(EmptyClass));
                string written = this.GetWrittenData();

                written.Should().Be("<root>");
            }

            [Fact]
            public void ShouldWriteTheOpeningElementForTheRootClass()
            {
                this.serializer.WriteBeginClass(nameof(EmptyClass));
                string written = this.GetWrittenData();

                written.Should().Be("<EmptyClass>");
            }
        }

        public sealed class WriteBeginProperty : XmlSerializerBaseTests
        {
            [Fact]
            public void ShouldWriteTheOpeningElement()
            {
                this.serializer.WriteBeginProperty("Property");
                string written = this.GetWrittenData();

                written.Should().Be("<Property>");
            }
        }

        public sealed class WriteElementSeparator : XmlSerializerBaseTests
        {
            [Fact]
            public void ShouldWriteANewElement()
            {
                this.XmlStreamWriter.WriteStartElement("root");
                this.serializer.WriteBeginArray(typeof(int), 1);
                this.ForceFullEndTag();

                this.serializer.WriteElementSeparator();
                string written = this.GetWrittenData();

                written.Should().Be("<root><int></int><int>");
            }
        }

        public sealed class WriteEndArray : XmlSerializerBaseTests
        {
            [Fact]
            public void ShouldCloseRootArrayTags()
            {
                this.serializer.WriteBeginArray(typeof(int), 1);
                this.ForceFullEndTag();

                this.serializer.WriteEndArray();
                string written = this.GetWrittenData();

                written.Should().Be("<ArrayOfint><int></int></ArrayOfint>");
            }

            [Fact]
            public void ShouldCloseTheCurrentXmlElement()
            {
                this.XmlStreamWriter.WriteStartElement("Array");
                this.ForceFullEndTag();

                this.serializer.WriteEndArray();
                string written = this.GetWrittenData();

                written.Should().Be("<Array></Array>");
            }

            [Fact]
            public void ShouldNotCloseOtherRootTags()
            {
                this.XmlStreamWriter.WriteStartElement("root");
                this.XmlStreamWriter.WriteStartElement("child");
                this.ForceFullEndTag();

                this.serializer.WriteEndArray();
                string written = this.GetWrittenData();

                written.Should().Be("<root><child></child>");
            }
        }

        public sealed class WriteEndClass : XmlSerializerBaseTests
        {
            [Fact]
            public void ShouldCloseTheRootClassXmlElement()
            {
                this.XmlStreamWriter.WriteStartElement(nameof(EmptyClass));
                this.ForceFullEndTag();

                this.serializer.WriteEndClass();
                string written = this.GetWrittenData();

                written.Should().Be("<EmptyClass></EmptyClass>");
            }

            [Fact]
            public void ShouldNotCloseNestedClasses()
            {
                this.XmlStreamWriter.WriteStartElement("root");
                this.XmlStreamWriter.WriteStartElement("property");
                this.ForceFullEndTag();

                this.serializer.WriteEndClass();
                string written = this.GetWrittenData();

                written.Should().Be("<root><property>");
            }
        }

        public sealed class WriteEndProperty : XmlSerializerBaseTests
        {
            [Fact]
            public void ShouldCloseTheCurrentXmlElement()
            {
                this.XmlStreamWriter.WriteStartElement("Property");
                this.ForceFullEndTag();

                this.serializer.WriteEndProperty();
                string written = this.GetWrittenData();

                written.Should().Be("<Property></Property>");
            }
        }

        private sealed class EmptyClass
        {
        }

        private sealed class FakeXmlSerializerBase : XmlSerializerBase
        {
            public FakeXmlSerializerBase(Stream stream, SerializationMode mode) : base(stream, mode)
            {
            }

            public FakeXmlSerializerBase(XmlSerializerBase parent) : base(parent)
            {
            }
        }
    }
}
