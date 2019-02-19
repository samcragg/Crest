namespace Host.UnitTests.Serialization.Xml
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization.Internal;
    using Crest.Host.Serialization.Xml;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class XmlFormatterSerializeTests
    {
        private readonly XmlFormatter formatter;
        private readonly MemoryStream stream = new MemoryStream();

        private XmlFormatterSerializeTests()
        {
            this.formatter = new XmlFormatter(this.stream, SerializationMode.Serialize);
        }

        private XmlStreamWriter XmlStreamWriter => (XmlStreamWriter)this.formatter.Writer;

        private void ForceFullEndTag()
        {
            this.XmlStreamWriter.WriteString(string.Empty);
        }

        private string GetWrittenData()
        {
            this.formatter.Writer.Flush();
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

        public sealed class Dispose : XmlFormatterSerializeTests
        {
            [Fact]
            public void ShouldNotDisposeTheStream()
            {
                Stream mockStream = Substitute.For<Stream>();
                var fakeSerializer = new XmlFormatter(mockStream, SerializationMode.Serialize);

                fakeSerializer.Dispose();

                ((IDisposable)mockStream).DidNotReceive().Dispose();
            }
        }

        public sealed class EnumsAsIntegers : XmlFormatterSerializeTests
        {
            [Fact]
            public void ShouldReturnFalse()
            {
                // This matches the DataContractSerializer
                this.formatter.EnumsAsIntegers.Should().BeFalse();
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
                return (string)XmlFormatter.GetMetadata(
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
                string result = (string)XmlFormatter.GetTypeMetadata(
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
                string result = (string)XmlFormatter.GetTypeMetadata(type);

                result.Should().Be(expected);
            }

            private class SpecialͣChar
            {
            }
        }

        public sealed class WriteBeginArray : XmlFormatterSerializeTests
        {
            [Fact]
            public void ShouldTransformPrimitiveElements()
            {
                this.XmlStreamWriter.WriteStartElement("root");

                this.formatter.WriteBeginArray(typeof(int), 1);
                string written = this.GetWrittenData();

                written.Should().StartWith("<root><int>");
            }

            [Fact]
            public void ShouldWriteTheRootElementIfNoneHasBeenWritten()
            {
                this.formatter.WriteBeginArray(typeof(EmptyClass), 1);
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

                this.formatter.WriteBeginClass((object)nameof(EmptyClass));
                string written = this.GetWrittenData();

                written.Should().Be("<root>");
            }

            [Fact]
            public void ShouldWriteTheOpeningElementForTheRootClass()
            {
                this.formatter.WriteBeginClass((object)nameof(EmptyClass));
                string written = this.GetWrittenData();

                written.Should().Be("<EmptyClass>");
            }
        }

        public sealed class WriteBeginPrimitive : XmlFormatterSerializeTests
        {
            [Fact]
            public void ShouldChangeWriteTheStartElement()
            {
                this.formatter.WriteBeginPrimitive("metadata");
                string written = this.GetWrittenData();

                written.Should().StartWith("<metadata>");
            }
        }

        public sealed class WriteBeginProperty : XmlFormatterSerializeTests
        {
            [Fact]
            public void ShouldWriteTheOpeningElement()
            {
                this.formatter.WriteBeginProperty((object)"Property");
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
                this.formatter.WriteBeginArray(typeof(int), 1);
                this.ForceFullEndTag();

                this.formatter.WriteElementSeparator();
                string written = this.GetWrittenData();

                written.Should().Be("<root><int></int><int>");
            }
        }

        public sealed class WriteEndArray : XmlFormatterSerializeTests
        {
            [Fact]
            public void ShouldCloseRootArrayTags()
            {
                this.formatter.WriteBeginArray(typeof(int), 1);
                this.ForceFullEndTag();

                this.formatter.WriteEndArray();
                string written = this.GetWrittenData();

                written.Should().Be("<ArrayOfint><int></int></ArrayOfint>");
            }

            [Fact]
            public void ShouldCloseTheCurrentXmlElement()
            {
                this.XmlStreamWriter.WriteStartElement("Array");
                this.ForceFullEndTag();

                this.formatter.WriteEndArray();
                string written = this.GetWrittenData();

                written.Should().Be("<Array></Array>");
            }

            [Fact]
            public void ShouldNotCloseOtherRootTags()
            {
                this.XmlStreamWriter.WriteStartElement("root");
                this.XmlStreamWriter.WriteStartElement("child");
                this.ForceFullEndTag();

                this.formatter.WriteEndArray();
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

                this.formatter.WriteEndClass();
                string written = this.GetWrittenData();

                written.Should().Be("<EmptyClass></EmptyClass>");
            }

            [Fact]
            public void ShouldNotCloseNestedClasses()
            {
                this.XmlStreamWriter.WriteStartElement("root");
                this.XmlStreamWriter.WriteStartElement("property");
                this.ForceFullEndTag();

                this.formatter.WriteEndClass();
                string written = this.GetWrittenData();

                written.Should().Be("<root><property>");
            }
        }

        public sealed class WriteEndPrimitive : XmlFormatterSerializeTests
        {
            [Fact]
            public void ShouldCloseTheXmlElement()
            {
                this.formatter.WriteBeginPrimitive("root");
                this.formatter.Writer.WriteInt32(1);

                this.formatter.WriteEndPrimitive();
                string written = this.GetWrittenData();

                written.Should().EndWith("</root>");
            }
        }

        public sealed class WriteEndProperty : XmlFormatterSerializeTests
        {
            [Fact]
            public void ShouldCloseTheCurrentXmlElement()
            {
                this.XmlStreamWriter.WriteStartElement("Property");
                this.ForceFullEndTag();

                this.formatter.WriteEndProperty();
                string written = this.GetWrittenData();

                written.Should().Be("<Property></Property>");
            }
        }

        private sealed class EmptyClass
        {
        }
    }
}
