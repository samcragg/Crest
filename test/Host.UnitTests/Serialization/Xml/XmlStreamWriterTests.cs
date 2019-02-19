namespace Host.UnitTests.Serialization.Xml
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml.Linq;
    using Crest.Host.Serialization.Xml;
    using FluentAssertions;
    using Xunit;

    public class XmlStreamWriterTests : IDisposable
    {
        private readonly MemoryStream stream = new MemoryStream();
        private readonly XmlStreamWriter writer;

        protected XmlStreamWriterTests()
        {
            this.writer = new XmlStreamWriter(this.stream);
        }

        public void Dispose()
        {
            this.stream.Dispose();
        }

        private string GetString<T>(Action<T> write, T value)
        {
            this.stream.SetLength(0);
            this.writer.WriteStartElement("root");
            write(value);
            this.writer.WriteEndElement();
            this.writer.Flush();

            string xml = Encoding.UTF8.GetString(this.stream.ToArray());
            return XDocument.Parse(xml).Root.Value;
        }

        public sealed class Depth : XmlStreamWriterTests
        {
            [Fact]
            public void ShouldDecreaseWhenAnElementHasBeenEnded()
            {
                this.writer.WriteStartElement("Element1");
                this.writer.WriteStartElement("Element2");
                this.writer.WriteEndElement();

                this.writer.Depth.Should().Be(1);
            }

            [Fact]
            public void ShouldIncreaseWhenAnElementHasBeenStarted()
            {
                this.writer.WriteStartElement("Element1");
                this.writer.WriteStartElement("Element2");

                this.writer.Depth.Should().Be(2);
            }

            [Fact]
            public void ShouldReturnZeroIfNothingHasBeenWritten()
            {
                this.writer.Depth.Should().Be(0);
            }
        }

        public sealed class DisposeTests : XmlStreamWriterTests
        {
            [Fact]
            public void ShouldNotDisposeTheStream()
            {
                this.writer.Dispose();

                // This will be set to false if the stream is disposed
                this.stream.CanRead.Should().BeTrue();
            }
        }

        public sealed class Flush : XmlStreamWriterTests
        {
            [Fact]
            public void ShouldWriteTheBufferToTheStream()
            {
                this.writer.WriteStartElement("Element");
                this.stream.Length.Should().Be(0);

                this.writer.Flush();
                this.stream.Length.Should().BeGreaterThan(0);
            }
        }

        public sealed class WriteBoolean : XmlStreamWriterTests
        {
            [Fact]
            public void ShouldWriteFalse()
            {
                string result = this.GetString(this.writer.WriteBoolean, false);

                result.Should().Be("false");
            }

            [Fact]
            public void ShouldWriteTrue()
            {
                string result = this.GetString(this.writer.WriteBoolean, true);

                result.Should().Be("true");
            }
        }

        public sealed class WriteChar : XmlStreamWriterTests
        {
            [Fact]
            public void ShouldOutputTheChar()
            {
                string result = this.GetString(this.writer.WriteChar, '<');

                // Because we parse the XML again, the XDocument will unescape
                // the character
                result.Should().Be("<");
            }
        }

        public sealed class WriteDateTime : XmlStreamWriterTests
        {
            // This tests the methods required by the base method to rent/commit
            // the buffer work. DateTime is chosen as it's simple and also
            // writes a smaller value than the buffer size requested.

            [Fact]
            public void ShouldTheValue()
            {
                var dateTime = new DateTime(2017, 1, 2, 13, 14, 15, DateTimeKind.Utc);

                string result = this.GetString(this.writer.WriteDateTime, dateTime);

                result.Should().Be("2017-01-02T13:14:15Z");
            }
        }

        public sealed class WriteNull : XmlStreamWriterTests
        {
            [Fact]
            public void ShouldWriteNull()
            {
                this.writer.WriteStartElement("root");
                this.writer.WriteNull();
                this.writer.WriteEndElement();
                this.writer.Flush();

                string xml = Encoding.UTF8.GetString(this.stream.ToArray());
                xml.Should().Contain("i:nil=\"true\"");
            }
        }

        public sealed class WriteString : XmlStreamWriterTests
        {
            [Fact]
            public void ShouldOutputTheString()
            {
                string result = this.GetString(this.writer.WriteString, @"Test<>Data");

                // Because we parse the XML again, the XDocument will unescape
                // the characters
                result.Should().Be(@"Test<>Data");
            }
        }
    }
}
