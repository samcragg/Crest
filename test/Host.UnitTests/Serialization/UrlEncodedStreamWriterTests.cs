namespace Host.UnitTests.Serialization
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class UrlEncodedStreamWriterTests : IDisposable
    {
        private readonly byte[] keyValue = new byte[] { (byte)'k', (byte)'e', (byte)'y' };
        private readonly MemoryStream stream = new MemoryStream();
        private readonly UrlEncodedStreamWriter writer;

        protected UrlEncodedStreamWriterTests()
        {
            this.writer = new UrlEncodedStreamWriter(this.stream);
        }

        public void Dispose()
        {
            this.stream.Dispose();
        }

        private string GetString<T>(Action<T> write, T value)
        {
            this.stream.SetLength(0);
            write(value);
            this.writer.Flush();
            return Encoding.UTF8.GetString(this.stream.ToArray());
        }

        public sealed class PopKeyPart : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldRemoveTheMostRecentlyAdded()
            {
                this.writer.PushKeyPart(1);
                this.writer.PushKeyPart(2);

                this.writer.PopKeyPart();
                string value = this.GetString(this.writer.WriteString, string.Empty);

                value.Should().Be("1=");
            }
        }

        public sealed class PushKeyPart : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldJoinPartsWithADot()
            {
                this.writer.PushKeyPart(1);
                this.writer.PushKeyPart(2);

                string value = this.GetString(this.writer.WriteString, string.Empty);

                value.Should().Be("1.2=");
            }

            [Fact]
            public void ShouldOutputArrayIndexes()
            {
                this.writer.PushKeyPart(12);

                string value = this.GetString(this.writer.WriteString, string.Empty);

                value.Should().Be("12=");
            }

            [Fact]
            public void ShouldOutputPropertyNames()
            {
                this.writer.PushKeyPart(new byte[] { (byte)'p' });

                string value = this.GetString(this.writer.WriteString, string.Empty);

                value.Should().Be("p=");
            }

            [Fact]
            public void ShouldWriteASeparatorBetweenKeyValues()
            {
                this.writer.PushKeyPart(1);
                this.writer.WriteString(string.Empty);

                string value = this.GetString(this.writer.WriteString, string.Empty);

                value.Should().Be("1=&1=");
            }
        }

        public sealed class WriteBoolean : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldWriteFalse()
            {
                string value = this.GetString(this.writer.WriteBoolean, false);

                value.Should().Be("false");
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteBoolean, true);

                value.Should().StartWith("key=");
            }

            [Fact]
            public void ShouldWriteTrue()
            {
                string value = this.GetString(this.writer.WriteBoolean, true);

                value.Should().Be("true");
            }
        }

        public sealed class WriteByte : UrlEncodedStreamWriterTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(byte.MaxValue)]
            public void ShouldWriteIntegerLimits(byte value)
            {
                string stringValue = this.GetString(this.writer.WriteByte, value);

                stringValue.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteByte, default(byte));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteChar : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldEncodeTheChar()
            {
                string result = this.GetString(this.writer.WriteChar, '+');

                result.Should().Be("%2B");
            }

            [Fact]
            public void ShouldOutputTheChar()
            {
                string result = this.GetString(this.writer.WriteChar, 'T');

                result.Should().Be("T");
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteChar, default(char));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteDateTime : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldWriteAnIso8601FormattedValue()
            {
                var dateTime = new DateTime(2017, 1, 2, 13, 14, 15, 16, DateTimeKind.Utc);

                string result = this.GetString(this.writer.WriteDateTime, dateTime);

                result.Should().Be("2017-01-02T13:14:15.0160000Z");
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteDateTime, default(DateTime));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteDecimal : UrlEncodedStreamWriterTests
        {
            [Theory]
            [InlineData("0")]
            [InlineData("-79228162514264337593543950335")] // MinValue
            [InlineData("79228162514264337593543950335")] // MaxValue
            public void ShouldWriteTheBounds(string value)
            {
                string result = this.GetString(
                    this.writer.WriteDecimal,
                    decimal.Parse(value, CultureInfo.InvariantCulture));

                result.Should().BeEquivalentTo(value);
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteDecimal, default(decimal));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteDouble : UrlEncodedStreamWriterTests
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

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteDouble, default(double));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteGuid : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldWriteTheHyphenatedValue()
            {
                const string GuidString = "F6CBC911-2025-4D99-A9CF-D86CF1CC809C";

                string value = this.GetString(this.writer.WriteGuid, new Guid(GuidString));

                value.Should().BeEquivalentTo(GuidString);
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteGuid, default(Guid));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteInt16 : UrlEncodedStreamWriterTests
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

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteInt16, default(short));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteInt32 : UrlEncodedStreamWriterTests
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

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteInt32, default(int));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteInt64 : UrlEncodedStreamWriterTests
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

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteInt64, default(long));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteNull : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldWriteNull()
            {
                this.writer.WriteNull();
                this.writer.Flush();
                string value = Encoding.UTF8.GetString(this.stream.ToArray());

                value.Should().Be("null");
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);
                this.writer.WriteNull();

                this.writer.Flush();
                string value = Encoding.UTF8.GetString(this.stream.ToArray());
                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteObject : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldUseCustomTypeConverters()
            {
                object testValue = new CustomValue("Example Text");

                string result = this.GetString(this.writer.WriteObject, testValue);

                result.Should().Be("Example+Text");
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteObject, new CustomValue(string.Empty));

                value.Should().StartWith("key=");
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

        public sealed class WriteSByte : UrlEncodedStreamWriterTests
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

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteSByte, default(sbyte));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteSingle : UrlEncodedStreamWriterTests
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

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteSingle, default(float));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteString : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldEncodeSpaces()
            {
                string result = this.GetString(this.writer.WriteString, "Test Data");

                result.Should().Be("Test+Data");
            }

            // Examples from Table 3-4 of the Unicode Standard 10.0
            [Theory]
            [InlineData("\u0040", "%40")]
            [InlineData("\u0430", "%D0%B0")]
            [InlineData("\u4e8c", "%E4%BA%8C")]
            [InlineData("\ud800\udf02", "%F0%90%8C%82")]
            public void ShouldEncodeUnicodeValues(string value, string escaped)
            {
                string result = this.GetString(this.writer.WriteString, value);

                result.Should().Be(escaped);
            }

            [Fact]
            public void ShouldOutputLongStrings()
            {
                // This tests the buffer gets flushed when it's full
                string longString = new string('a', 2000);

                string result = this.GetString(this.writer.WriteString, longString);

                result.Should().Be(longString);
            }

            [Fact]
            public void ShouldOutputTheString()
            {
                string result = this.GetString(this.writer.WriteString, "Test_*.-");

                result.Should().Be("Test_*.-");
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteString, string.Empty);

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteTimeSpan : UrlEncodedStreamWriterTests
        {
            [Fact]
            public void ShouldWriteAnIso8601FormattedValue()
            {
                var time = new TimeSpan(12, 3, 4, 5);

                string result = this.GetString(this.writer.WriteTimeSpan, time);

                result.Should().BeEquivalentTo("P12DT3H4M5S");
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteTimeSpan, default(TimeSpan));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteUInt16 : UrlEncodedStreamWriterTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(ushort.MaxValue)]
            public void ShouldWriteIntegerLimits(ushort value)
            {
                string stringValue = this.GetString(this.writer.WriteUInt16, value);

                stringValue.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteUInt16, default(ushort));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteUInt32 : UrlEncodedStreamWriterTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(uint.MaxValue)]
            public void ShouldWriteIntegerLimits(uint value)
            {
                string stringValue = this.GetString(this.writer.WriteUInt32, value);

                stringValue.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteUInt32, default(uint));

                value.Should().StartWith("key=");
            }
        }

        public sealed class WriteUInt64 : UrlEncodedStreamWriterTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(ulong.MaxValue)]
            public void ShouldWriteIntegerLimits(ulong value)
            {
                string stringValue = this.GetString(this.writer.WriteUInt64, value);

                stringValue.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }

            [Fact]
            public void ShouldWriteThePropertyName()
            {
                this.writer.PushKeyPart(this.keyValue);

                string value = this.GetString(this.writer.WriteUInt64, default(ulong));

                value.Should().StartWith("key=");
            }
        }
    }
}
