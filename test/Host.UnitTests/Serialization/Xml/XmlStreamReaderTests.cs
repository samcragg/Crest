namespace Host.UnitTests.Serialization.Xml
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class XmlStreamReaderTests
    {
        private static T ReadValue<T>(string contents, Func<XmlStreamReader, T> readMethod)
        {
            return ReadXmlValue("<a>" + contents + "</a>", readMethod);
        }

        private static T ReadXmlValue<T>(string xml, Func<XmlStreamReader, T> readMethod)
        {
            T value = default;
            WithReader(xml, r =>
            {
                r.ReadStartElement();
                value = readMethod(r);
            });
            return value;
        }

        private static void WithReader(string xml, Action<XmlStreamReader> action)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(xml);
            using (var stream = new MemoryStream(bytes, writable: false))
            {
                var reader = new XmlStreamReader(stream);
                action(reader);
            }
        }

        public sealed class CanReadStartElement : XmlStreamReaderTests
        {
            [Fact]
            public void ShouldReadFalseIfNotAtTheStartOfAnElement()
            {
                const string Xml =
@"<first></first>";

                WithReader(Xml, reader =>
                {
                    reader.ReadStartElement();
                    bool result = reader.CanReadStartElement();

                    result.Should().BeFalse();
                });
            }

            [Fact]
            public void ShouldReadTrueIfAtTheStartOfAnElement()
            {
                const string Xml =
@"<first></first>";

                WithReader(Xml, reader =>
                {
                    bool result = reader.CanReadStartElement();

                    result.Should().BeTrue();
                });
            }
        }

        public sealed class GetCurrentPosition : XmlStreamReaderTests
        {
            private const string Xml =
@"<line1>
  <line2 />
</line1>";

            [Fact]
            public void ShouldReturnTheColumnInformation()
            {
                WithReader(Xml, reader =>
                {
                    reader.ReadStartElement();

                    string result = reader.GetCurrentPosition();

                    // The position will be inside the tag and one-based:
                    // "  <line 2/>"
                    //     |
                    //  1234
                    result.Should().MatchEquivalentOf("*column: 4*");
                });
            }

            [Fact]
            public void ShouldReturnTheLineInformation()
            {
                WithReader(Xml, reader =>
                {
                    reader.ReadStartElement();

                    string result = reader.GetCurrentPosition();

                    result.Should().MatchEquivalentOf("*line: 2*");
                });
            }
        }

        public sealed class ReadBoolean : XmlStreamReaderTests
        {
            [Fact]
            public void ShouldIgnoreWhiteSpace()
            {
                bool result = ReadValue("\t true\r\n", r => r.ReadBoolean());

                result.Should().BeTrue();
            }

            [Theory]
            [InlineData("false")]
            [InlineData("0")]
            public void ShouldReturnFalseForTheFalseTokens(string value)
            {
                bool result = ReadValue(value, r => r.ReadBoolean());

                result.Should().BeFalse();
            }

            [Theory]
            [InlineData("true")]
            [InlineData("1")]
            public void ShouldReturnTrueForTheTrueTokens(string value)
            {
                bool result = ReadValue(value, r => r.ReadBoolean());

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldThrowForEmptyElements()
            {
                Action action = () => ReadXmlValue("<a/>", r => r.ReadBoolean());

                action.Should().Throw<FormatException>();
            }

            [Fact]
            public void ShouldThrowForInvalidTokens()
            {
                Action action = () => ReadValue("invalid", r => r.ReadBoolean());

                // Make sure the exception includes where it was
                action.Should().Throw<FormatException>().WithMessage("*1*");
            }
        }

        public sealed class ReadChar : XmlStreamReaderTests
        {
            [Theory]
            [InlineData("X", 'X')]
            [InlineData("&lt;", '<')]
            public void ShouldReturnTheCharacter(string value, char expected)
            {
                char result = ReadValue(value, r => r.ReadChar());

                result.Should().Be(expected);
            }

            [Fact]
            public void ShouldThrowForMissingCharacters()
            {
                Action action = () => ReadXmlValue("<empty />", r => r.ReadChar());

                action.Should().Throw<FormatException>();
            }

            [Fact]
            public void ShouldThrowForTooManyCharacters()
            {
                Action action = () => ReadValue("invalid", r => r.ReadChar());

                action.Should().Throw<FormatException>();
            }
        }

        public sealed class ReadEndElement : XmlStreamReaderTests
        {
            [Fact]
            public void ShouldHandleEmptyElements()
            {
                const string Xml =
@"<root>
  <element />
  text
</root>";

                WithReader(Xml, reader =>
                {
                    reader.ReadStartElement().Should().Be("root");
                    reader.ReadStartElement().Should().Be("element");
                    reader.ReadEndElement();

                    string result = reader.ReadString().Trim();

                    result.Should().Be("text");
                });
            }

            [Fact]
            public void ShouldThrowIfNoEndElement()
            {
                const string Xml = @"<element></element>";

                WithReader(Xml, reader =>
                {
                    reader.ReadStartElement();
                    reader.ReadEndElement();

                    Action action = () => reader.ReadEndElement();

                    action.Should().Throw<FormatException>();
                });
            }
        }

        public sealed class ReadNull : XmlStreamReaderTests
        {
            private const string XmlNamespaceAttribute = @"xmlns:i='http://www.w3.org/2001/XMLSchema-instance'";

            [Fact]
            public void ShouldReturnFalseIfTheNilAttributeIsFalse()
            {
                bool result = ReadXmlValue(
                    "<a " + XmlNamespaceAttribute + " ignore='true' i:nil='false'/>",
                    r => r.ReadNull());

                result.Should().Be(false);
            }

            [Fact]
            public void ShouldReturnFalseIfThereIsNoNilAttribute()
            {
                bool result = ReadXmlValue(
                    "<a nil='true' other='true' />",
                    r => r.ReadNull());

                result.Should().Be(false);
            }

            [Fact]
            public void ShouldReturnTrueIfTheNilAttributeIsTrue()
            {
                bool result = ReadXmlValue(
                    "<a " + XmlNamespaceAttribute + " ignore='false' i:nil='true'/>",
                    r => r.ReadNull());

                result.Should().Be(true);
            }
        }

        public sealed class ReadStartElement : XmlStreamReaderTests
        {
            [Fact]
            public void ShouldReadTheNextElement()
            {
                const string Xml =
@"<first></first>
  <!-- Comment -->
  <second />";

                WithReader(Xml, reader =>
                {
                    reader.ReadStartElement();
                    reader.ReadEndElement();

                    string result = reader.ReadStartElement();

                    result.Should().Be("second");
                });
            }

            [Fact]
            public void ShouldSkipTheXmlDeclaration()
            {
                const string Xml =
@"<?xml version='1.0'?>
  <element />";

                WithReader(Xml, reader =>
                {
                    string result = reader.ReadStartElement();

                    result.Should().Be("element");
                });
            }

            [Fact]
            public void ShouldThrowIfNoStartingElement()
            {
                const string Xml =
@"<?xml version='1.0'?>
  <first />";

                WithReader(Xml, reader =>
                {
                    reader.ReadStartElement();

                    Action action = () => reader.ReadStartElement();

                    action.Should().Throw<FormatException>();
                });
            }
        }

        public sealed class ReadString : XmlStreamReaderTests
        {
            [Fact]
            public void ShouldReadCDataContent()
            {
                string result = ReadXmlValue("<a><![CDATA[text]]></a>", r => r.ReadString());

                result.Should().Be("text");
            }

            [Fact]
            public void ShouldReadEmptyElements()
            {
                string result = ReadXmlValue("<empty/>", r => r.ReadString());

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReadMixedContent()
            {
                string result = ReadXmlValue("<a>one <![CDATA[two]]></a>", r => r.ReadString());

                result.Should().Be("one two");
            }

            [Fact]
            public void ShouldReadTextElements()
            {
                string result = ReadXmlValue("<a>text</a>", r => r.ReadString());

                result.Should().Be("text");
            }
        }
    }
}
