namespace Host.UnitTests.Serialization
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml.Linq;
    using Crest.Host.Serialization;
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
                string value = this.GetString(this.writer.WriteBoolean, false);

                value.Should().Be("false");
            }

            [Fact]
            public void ShouldWriteTrue()
            {
                string value = this.GetString(this.writer.WriteBoolean, true);

                value.Should().Be("true");
            }
        }

        public sealed class WriteByte : XmlStreamWriterTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(byte.MaxValue)]
            public void ShouldWriteIntegerLimits(byte value)
            {
                string stringValue = this.GetString(this.writer.WriteByte, value);

                stringValue.Should().Be(value.ToString(CultureInfo.InvariantCulture));
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
            [Fact]
            public void ShouldWriteAnIso8601FormattedValue()
            {
                var dateTime = new DateTime(2017, 1, 2, 13, 14, 15, 16, DateTimeKind.Utc);

                string result = this.GetString(this.writer.WriteDateTime, dateTime);

                result.Should().Be("2017-01-02T13:14:15.0160000Z");
            }
        }

        public sealed class WriteDecimal : XmlStreamWriterTests
        {
            [Theory]
            [InlineData("0")]
            [InlineData("-79228162514264337593543950335")] // MinValue
            [InlineData("79228162514264337593543950335")] // MaxValue
            public void ShouldWriteTheBounds(string value)
            {
                string result = this.GetString(this.writer.WriteDecimal, decimal.Parse(value));

                result.Should().BeEquivalentTo(value);
            }
        }

        public sealed class WriteDouble : XmlStreamWriterTests
        {
            [Theory]
            [InlineData(123e200)]
            [InlineData(-123e200)]
            [InlineData(123e-200)]
            [InlineData(-123e-200)]
            [InlineData(123.4)]
            [InlineData(double.NaN)]
            [InlineData(double.NegativeInfinity)]
            [InlineData(double.PositiveInfinity)]
            public void ShouldWriteTheNumber(double value)
            {
                string result = this.GetString(this.writer.WriteDouble, value);

                result.Should().BeEquivalentTo(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteGuid : XmlStreamWriterTests
        {
            [Fact]
            public void ShouldWriteTheHyphenatedValue()
            {
                const string GuidString = "F6CBC911-2025-4D99-A9CF-D86CF1CC809C";

                string value = this.GetString(this.writer.WriteGuid, new Guid(GuidString));

                value.Should().BeEquivalentTo(GuidString);
            }
        }

        public sealed class WriteInt16 : XmlStreamWriterTests
        {
            [Theory]
            [InlineData(short.MinValue)]
            [InlineData(0)]
            [InlineData(short.MaxValue)]
            public void ShouldWriteIntegerLimits(short value)
            {
                string stringValue = this.GetString(this.writer.WriteInt16, value);

                stringValue.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteInt32 : XmlStreamWriterTests
        {
            [Theory]
            [InlineData(int.MinValue)]
            [InlineData(0)]
            [InlineData(int.MaxValue)]
            public void ShouldWriteIntegerLimits(int value)
            {
                string stringValue = this.GetString(this.writer.WriteInt32, value);

                stringValue.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteInt64 : XmlStreamWriterTests
        {
            [Theory]
            [InlineData(long.MinValue)]
            [InlineData(0)]
            [InlineData(long.MaxValue)]
            public void ShouldWriteIntegerLimits(long value)
            {
                string stringValue = this.GetString(this.writer.WriteInt64, value);

                stringValue.Should().Be(value.ToString(CultureInfo.InvariantCulture));
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

        public sealed class WriteObject : XmlStreamWriterTests
        {
            [Fact]
            public void ShouldUseCustomTypeConverters()
            {
                object testValue = new CustomValue("Example<>Text");

                string result = this.GetString(this.writer.WriteObject, testValue);

                result.Should().Be("Example<>Text");
            }

            [TypeConverter(typeof(CustomValueConverter))]
            private class CustomValue
            {
                public CustomValue(string value)
                {
                    this.Value = value;
                }

                public string Value { get; }
            }

            private class CustomValueConverter : TypeConverter
            {
                public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
                {
                    return ((CustomValue)value).Value;
                }
            }
        }

        public sealed class WriteSByte : XmlStreamWriterTests
        {
            [Theory]
            [InlineData(sbyte.MinValue)]
            [InlineData(0)]
            [InlineData(sbyte.MaxValue)]
            public void ShouldWriteIntegerLimits(sbyte value)
            {
                string stringValue = this.GetString(this.writer.WriteSByte, value);

                stringValue.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteSingle : XmlStreamWriterTests
        {
            [Theory]
            [InlineData(123e21f)]
            [InlineData(-123e21f)]
            [InlineData(123e-21f)]
            [InlineData(-123e-21f)]
            [InlineData(123.4f)]
            [InlineData(float.NaN)]
            [InlineData(float.NegativeInfinity)]
            [InlineData(float.PositiveInfinity)]
            public void ShouldWriteTheNumber(float value)
            {
                string result = this.GetString(this.writer.WriteSingle, value);

                result.Should().BeEquivalentTo(value.ToString(CultureInfo.InvariantCulture));
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

        public sealed class WriteTimeSpan : XmlStreamWriterTests
        {
            [Fact]
            public void ShouldWriteAnIso8601FormattedValue()
            {
                var time = new TimeSpan(12, 3, 4, 5);

                string result = this.GetString(this.writer.WriteTimeSpan, time);

                result.Should().BeEquivalentTo("P12DT3H4M5S");
            }
        }

        public sealed class WriteUInt16 : XmlStreamWriterTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(ushort.MaxValue)]
            public void ShouldWriteIntegerLimits(ushort value)
            {
                string stringValue = this.GetString(this.writer.WriteUInt16, value);

                stringValue.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteUInt32 : XmlStreamWriterTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(uint.MaxValue)]
            public void ShouldWriteIntegerLimits(uint value)
            {
                string stringValue = this.GetString(this.writer.WriteUInt32, value);

                stringValue.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteUInt64 : XmlStreamWriterTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(ulong.MaxValue)]
            public void ShouldWriteIntegerLimits(ulong value)
            {
                string stringValue = this.GetString(this.writer.WriteUInt64, value);

                stringValue.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
