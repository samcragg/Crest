namespace Host.UnitTests.Serialization
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Text;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using Xunit;

    public class ValueWriterTests
    {
        private string GetString(Action<ValueWriter> write)
        {
            var writer = new FakeValueWriter();
            write(writer);
            return Encoding.UTF8.GetString(writer.Bytes);
        }

        public sealed class WriteByte : ValueWriterTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(byte.MaxValue)]
            public void ShouldWriteIntegerLimits(byte value)
            {
                string result = this.GetString(w => w.WriteByte(value));

                result.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteDateTime : ValueWriterTests
        {
            [Fact]
            public void ShouldWriteAnIso8601FormattedValue()
            {
                var dateTime = new DateTime(2017, 1, 2, 13, 14, 15, 16, DateTimeKind.Utc);

                string result = this.GetString(w => w.WriteDateTime(dateTime));

                result.Should().Be("2017-01-02T13:14:15.0160000Z");
            }
        }

        public sealed class WriteDecimal : ValueWriterTests
        {
            [Theory]
            [InlineData("0")]
            [InlineData("-79228162514264337593543950335")] // MinValue
            [InlineData("79228162514264337593543950335")] // MaxValue
            public void ShouldWriteTheBounds(string value)
            {
                decimal d = decimal.Parse(value, CultureInfo.InvariantCulture);

                string result = this.GetString(w => w.WriteDecimal(d));

                result.Should().BeEquivalentTo(value);
            }
        }

        public sealed class WriteDouble : ValueWriterTests
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
                string result = this.GetString(w => w.WriteDouble(value));

                result.Should().BeEquivalentTo(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteGuid : ValueWriterTests
        {
            [Fact]
            public void ShouldWriteTheHyphenatedValue()
            {
                const string GuidString = "F6CBC911-2025-4D99-A9CF-D86CF1CC809C";

                string value = this.GetString(w => w.WriteGuid(new Guid(GuidString)));

                value.Should().BeEquivalentTo(GuidString);
            }
        }

        public sealed class WriteInt16 : ValueWriterTests
        {
            [Theory]
            [InlineData(short.MinValue)]
            [InlineData(0)]
            [InlineData(short.MaxValue)]
            public void ShouldWriteIntegerLimits(short value)
            {
                string result = this.GetString(w => w.WriteInt16(value));

                result.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteInt32 : ValueWriterTests
        {
            [Theory]
            [InlineData(int.MinValue)]
            [InlineData(0)]
            [InlineData(int.MaxValue)]
            public void ShouldWriteIntegerLimits(int value)
            {
                string result = this.GetString(w => w.WriteInt32(value));

                result.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteInt64 : ValueWriterTests
        {
            [Theory]
            [InlineData(long.MinValue)]
            [InlineData(0)]
            [InlineData(long.MaxValue)]
            public void ShouldWriteIntegerLimits(long value)
            {
                string result = this.GetString(w => w.WriteInt64(value));

                result.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteObject : ValueWriterTests
        {
            [Fact]
            public void ShouldUseCustomTypeConverters()
            {
                object testValue = new CustomValue("Example Text");

                string result = this.GetString(w => w.WriteObject(testValue));

                result.Should().Be("Example Text");
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

        public sealed class WriteSByte : ValueWriterTests
        {
            [Theory]
            [InlineData(sbyte.MinValue)]
            [InlineData(0)]
            [InlineData(sbyte.MaxValue)]
            public void ShouldWriteIntegerLimits(sbyte value)
            {
                string result = this.GetString(w => w.WriteSByte(value));

                result.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteSingle : ValueWriterTests
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
                string result = this.GetString(w => w.WriteSingle(value));

                result.Should().BeEquivalentTo(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteTimeSpan : ValueWriterTests
        {
            [Fact]
            public void ShouldWriteAnIso8601FormattedValue()
            {
                var time = new TimeSpan(12, 3, 4, 5);

                string result = this.GetString(w => w.WriteTimeSpan(time));

                result.Should().BeEquivalentTo("P12DT3H4M5S");
            }
        }

        public sealed class WriteUInt16 : ValueWriterTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(ushort.MaxValue)]
            public void ShouldWriteIntegerLimits(ushort value)
            {
                string result = this.GetString(w => w.WriteUInt16(value));

                result.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteUInt32 : ValueWriterTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(uint.MaxValue)]
            public void ShouldWriteIntegerLimits(uint value)
            {
                string result = this.GetString(w => w.WriteUInt32(value));

                result.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        public sealed class WriteUInt64 : ValueWriterTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(ulong.MaxValue)]
            public void ShouldWriteIntegerLimits(ulong value)
            {
                string result = this.GetString(w => w.WriteUInt64(value));

                result.Should().Be(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        private class FakeValueWriter : ValueWriter
        {
            private byte[] buffer;

            internal byte[] Bytes { get; private set; }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override void WriteBoolean(bool value)
            {
                throw new NotImplementedException();
            }

            public override void WriteChar(char value)
            {
                throw new NotImplementedException();
            }

            public override void WriteNull()
            {
                throw new NotImplementedException();
            }

            public override void WriteString(string value)
            {
                this.Bytes = Encoding.UTF8.GetBytes(value);
            }

            protected override void CommitBuffer(int bytes)
            {
                Array.Resize(ref this.buffer, bytes);
                this.Bytes = this.buffer;
            }

            protected override ArraySegment<byte> RentBuffer(int maximumSize)
            {
                this.buffer = new byte[maximumSize];
                return new ArraySegment<byte>(this.buffer);
            }
        }
    }
}
