﻿namespace Host.UnitTests.Serialization.Xml
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class XmlFormatterSerializeTests
    {
        private readonly XmlSerializerBase serializer;
        private readonly MemoryStream stream = new MemoryStream();

        private XmlFormatterSerializeTests()
        {
            this.serializer = new FakeXmlSerializerBase(this.stream);
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

        public sealed class BeginWrite : XmlFormatterSerializeTests
        {
            [Fact]
            public void ShouldChangeWriteTheStartElement()
            {
                this.serializer.BeginWrite("metadata");
                string written = this.GetWrittenData();

                written.Should().StartWith("<metadata>");
            }
        }

        public sealed class Constructor : XmlFormatterSerializeTests
        {
            [Fact]
            public void ShouldCreateAStreamWriter()
            {
                var instance = new FakeXmlSerializerBase(Stream.Null);

                instance.Writer.Should().NotBeNull();
            }

            [Fact]
            public void ShouldSetTheWriter()
            {
                var parent = new FakeXmlSerializerBase(Stream.Null);

                var instance = new FakeXmlSerializerBase(parent);

                instance.Writer.Should().BeSameAs(parent.Writer);
            }
        }

        public sealed class EndWrite : XmlFormatterSerializeTests
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

        public sealed class Flush : XmlFormatterSerializeTests
        {
            [Fact]
            public void ShouldFlushTheBuffers()
            {
                Stream stream = Substitute.For<Stream>();
                XmlSerializerBase serializer = new FakeXmlSerializerBase(stream);

                serializer.BeginWrite("root");
                stream.DidNotReceiveWithAnyArgs().Write(null, 0, 0);

                serializer.Flush();
                stream.ReceivedWithAnyArgs().Write(null, 0, 0);
            }
        }

        public sealed class GetMetadata : XmlFormatterSerializeTests
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

        public sealed class GetTypeMetadata : XmlFormatterSerializeTests
        {
            [Fact]
            public void ShouldEscapeCharactersFromTheDisplayName()
            {
                // Unicode 0x363 is valid for C# but not for XML
                string result = XmlSerializerBase.GetTypeMetadata(
                    typeof(SpecialͣChar));

                result.Should().Be("Special_x0363_Char");
            }

            // http://www.w3.org/TR/xmlschema11-2/
            [Theory]
            [InlineData(typeof(bool), "boolean")]
            [InlineData(typeof(byte), "unsignedByte")]
            [InlineData(typeof(DateTime), "dateTime")]
            [InlineData(typeof(decimal), "decimal")]
            [InlineData(typeof(double), "double")]
            [InlineData(typeof(float), "float")]
            [InlineData(typeof(int), "int")]
            [InlineData(typeof(long), "long")]
            [InlineData(typeof(sbyte), "byte")]
            [InlineData(typeof(short), "short")]
            [InlineData(typeof(string), "string")]
            [InlineData(typeof(uint), "unsignedInt")]
            [InlineData(typeof(ulong), "unsignedLong")]
            [InlineData(typeof(ushort), "unsignedShort")]
            [InlineData(typeof(TimeSpan), "duration")]
            public void ShouldReturnXmlSchemaNames(Type type, string expected)
            {
                string result = XmlSerializerBase.GetTypeMetadata(type);

                result.Should().Be(expected);
            }

            private class SpecialͣChar
            {
            }
        }

        public sealed class OutputEnumNames : XmlFormatterSerializeTests
        {
            [Fact]
            public void ShouldReturnTrue()
            {
                // This matches the DataContractSerializer
                XmlSerializerBase.OutputEnumNames.Should().BeTrue();
            }
        }

        public sealed class WriteBeginArray : XmlFormatterSerializeTests
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

        public sealed class WriteBeginClass : XmlFormatterSerializeTests
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

        public sealed class WriteBeginProperty : XmlFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteTheOpeningElement()
            {
                this.serializer.WriteBeginProperty("Property");
                string written = this.GetWrittenData();

                written.Should().Be("<Property>");
            }
        }

        public sealed class WriteElementSeparator : XmlFormatterSerializeTests
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

        public sealed class WriteEndArray : XmlFormatterSerializeTests
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

        public sealed class WriteEndClass : XmlFormatterSerializeTests
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

        public sealed class WriteEndProperty : XmlFormatterSerializeTests
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
            public FakeXmlSerializerBase(Stream stream) : base(stream, SerializationMode.Serialize)
            {
            }

            public FakeXmlSerializerBase(XmlSerializerBase parent) : base(parent)
            {
            }
        }
    }
}
