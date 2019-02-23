namespace Host.UnitTests.Serialization.Internal
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using Crest.Host.Serialization.Internal;
    using FluentAssertions;
    using Host.UnitTests.TestHelpers;
    using Xunit;

    public class ValueReaderTests
    {
        private static T ReadValue<T>(string contents, Func<ValueReader, T> readMethod)
        {
            var reader = new FakeValueReader
            {
                CurrentContents = contents
            };

            return readMethod(reader);
        }

        public sealed class ReadByte : ValueReaderTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(byte.MaxValue)]
            public void ShouldReadIntegerLimits(byte value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                byte result = ReadValue(stringValue, r => r.ReadByte());

                result.Should().Be(value);
            }

            [Theory]
            [InlineData(-1)]
            [InlineData(byte.MaxValue + 1)]
            public void ShouldThrowForOutOfRange(int value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                Action action = () => ReadValue(stringValue, r => r.ReadByte());

                action.Should().Throw<FormatException>()
                      .WithMessage("*" + FakeValueReader.Position + "*");
            }
        }

        public sealed class ReadDateTime : ValueReaderTests
        {
            [Fact]
            public void ShouldReadIsoDateTimes()
            {
                DateTime result = ReadValue("2017-12-31T13:14:15-01:00", r => r.ReadDateTime());

                result.Should().Be(new DateTime(2017, 12, 31, 14, 14, 15, DateTimeKind.Utc));
            }

            [Fact]
            public void ShouldThrowForInvalidDates()
            {
                Action action = () => ReadValue("invalid", r => r.ReadDateTime());

                action.Should().Throw<FormatException>()
                      .WithMessage("*" + FakeValueReader.Position + "*");
            }
        }

        public sealed class ReadDecimal : ValueReaderTests
        {
            [Theory]
            [InlineData("-79228162514264337593543950335")]
            [InlineData("0")]
            [InlineData("79228162514264337593543950335")]
            public void ShouldReadTheLimits(string value)
            {
                decimal expected = decimal.Parse(value, CultureInfo.InvariantCulture);

                decimal result = ReadValue(value, r => r.ReadDecimal());

                result.Should().Be(expected);
            }
        }

        public sealed class ReadDouble : ValueReaderTests
        {
            [Theory]
            [InlineData(double.MinValue)]
            [InlineData(0)]
            [InlineData(double.MaxValue)]
            public void ShouldReadTheLimits(double value)
            {
                string stringValue = value.ToString("r", CultureInfo.InvariantCulture);

                double result = ReadValue(stringValue, r => r.ReadDouble());

                result.Should().Be(value);
            }
        }

        public sealed class ReadGuid : ValueReaderTests
        {
            [Fact]
            public void ShouldReadGuidStrings()
            {
                const string GuidString = "7DBE08AD-4460-453F-ABF6-CA6E25235083";

                Guid result = ReadValue(GuidString, r => r.ReadGuid());

                result.Should().Be(new Guid(GuidString));
            }

            [Fact]
            public void ShouldThrowForInvalidGuids()
            {
                Action action = () => ReadValue("invalid", r => r.ReadGuid());

                action.Should().Throw<FormatException>()
                      .WithMessage("*" + FakeValueReader.Position + "*");
            }
        }

        public sealed class ReadInt16 : ValueReaderTests
        {
            [Theory]
            [InlineData(short.MinValue)]
            [InlineData(0)]
            [InlineData(short.MaxValue)]
            public void ShouldReadIntegerLimits(short value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                short result = ReadValue(stringValue, r => r.ReadInt16());

                result.Should().Be(value);
            }

            [Theory]
            [InlineData(short.MinValue - 1)]
            [InlineData(short.MaxValue + 1)]
            public void ShouldThrowForOutOfRange(int value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                Action action = () => ReadValue(stringValue, r => r.ReadInt16());

                action.Should().Throw<FormatException>()
                      .WithMessage("*" + FakeValueReader.Position + "*");
            }
        }

        public sealed class ReadInt32 : ValueReaderTests
        {
            [Theory]
            [InlineData(int.MinValue)]
            [InlineData(0)]
            [InlineData(int.MaxValue)]
            public void ShouldReadIntegerLimits(int value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                int result = ReadValue(stringValue, r => r.ReadInt32());

                result.Should().Be(value);
            }

            [Theory]
            [InlineData(int.MinValue - 1L)]
            [InlineData(int.MaxValue + 1L)]
            public void ShouldThrowForOutOfRange(long value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                Action action = () => ReadValue(stringValue, r => r.ReadInt32());

                action.Should().Throw<FormatException>()
                      .WithMessage("*" + FakeValueReader.Position + "*");
            }
        }

        public sealed class ReadInt64 : ValueReaderTests
        {
            [Theory]
            [InlineData(long.MinValue)]
            [InlineData(0)]
            [InlineData(long.MaxValue)]
            public void ShouldReadIntegerLimits(long value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                long result = ReadValue(stringValue, r => r.ReadInt64());

                result.Should().Be(value);
            }
        }

        public sealed class ReadObject : ValueReaderTests
        {
            [Fact]
            public void ShouldUseCustomTypeConverters()
            {
                object result = ReadValue(" string ", r => r.ReadObject(typeof(ClassWithCustomTypeConverter)));

                result.Should().BeOfType<ClassWithCustomTypeConverter>()
                      .Which.Value.Should().Be(" string ");
            }
        }

        public sealed class ReadSByte : ValueReaderTests
        {
            [Theory]
            [InlineData(sbyte.MinValue)]
            [InlineData(0)]
            [InlineData(sbyte.MaxValue)]
            public void ShouldReadIntegerLimits(sbyte value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                sbyte result = ReadValue(stringValue, r => r.ReadSByte());

                result.Should().Be(value);
            }

            [Theory]
            [InlineData(sbyte.MinValue - 1)]
            [InlineData(sbyte.MaxValue + 1)]
            public void ShouldThrowForOutOfRange(int value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                Action action = () => ReadValue(stringValue, r => r.ReadSByte());

                action.Should().Throw<FormatException>()
                      .WithMessage("*" + FakeValueReader.Position + "*");
            }
        }

        public sealed class ReadSingle : ValueReaderTests
        {
            [Theory]
            [InlineData(float.MinValue)]
            [InlineData(0)]
            [InlineData(float.MaxValue)]
            public void ShouldReadTheLimits(float value)
            {
                string stringValue = value.ToString("r", CultureInfo.InvariantCulture);

                float result = ReadValue(stringValue, r => r.ReadSingle());

                result.Should().Be(value);
            }
        }

        public sealed class ReadTimeSpan : ValueReaderTests
        {
            [Fact]
            public void ShouldReadDotNetTimeSpans()
            {
                TimeSpan result = ReadValue("1.2:03:04.0050000", r => r.ReadTimeSpan());

                result.Should().BeCloseTo(new TimeSpan(1, 2, 3, 4, 5));
            }

            [Fact]
            public void ShouldReadIsoTimeSpans()
            {
                TimeSpan result = ReadValue("P1DT2H3M4.005S", r => r.ReadTimeSpan());

                result.Should().BeCloseTo(new TimeSpan(1, 2, 3, 4, 5));
            }

            [Fact]
            public void ShouldThrowForInvalidStrings()
            {
                Action action = () => ReadValue("invalid", r => r.ReadTimeSpan());

                action.Should().Throw<FormatException>()
                      .WithMessage("*" + FakeValueReader.Position + "*");
            }
        }

        public sealed class ReadUInt16 : ValueReaderTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(ushort.MaxValue)]
            public void ShouldReadIntegerLimits(ushort value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                ushort result = ReadValue(stringValue, r => r.ReadUInt16());

                result.Should().Be(value);
            }

            [Theory]
            [InlineData(-1)]
            [InlineData(ushort.MaxValue + 1)]
            public void ShouldThrowForOutOfRange(int value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                Action action = () => ReadValue(stringValue, r => r.ReadUInt16());

                action.Should().Throw<FormatException>()
                      .WithMessage("*" + FakeValueReader.Position + "*");
            }
        }

        public sealed class ReadUInt32 : ValueReaderTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(uint.MaxValue)]
            public void ShouldReadIntegerLimits(uint value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                uint result = ReadValue(stringValue, r => r.ReadUInt32());

                result.Should().Be(value);
            }

            [Theory]
            [InlineData(-1)]
            [InlineData(uint.MaxValue + 1L)]
            public void ShouldThrowForOutOfRange(long value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                Action action = () => ReadValue(stringValue, r => r.ReadUInt32());

                action.Should().Throw<FormatException>()
                      .WithMessage("*" + FakeValueReader.Position + "*");
            }
        }

        public sealed class ReadUInt64 : ValueReaderTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(ulong.MaxValue)]
            public void ShouldReadIntegerLimits(ulong value)
            {
                string stringValue = value.ToString(CultureInfo.InvariantCulture);

                ulong result = ReadValue(stringValue, r => r.ReadUInt64());

                result.Should().Be(value);
            }
        }

        public sealed class ReadUri : ValueReaderTests
        {
            [Fact]
            public void ShouldReadAbsoluteUris()
            {
                const string Address = "http://www.example.com/";

                Uri result = ReadValue(Address, r => r.ReadUri());

                result.IsAbsoluteUri.Should().BeTrue();
                result.AbsoluteUri.Should().Be(Address);
            }

            [Fact]
            public void ShouldReadReltiveUris()
            {
                const string Address = "/path";

                Uri result = ReadValue(Address, r => r.ReadUri());

                result.IsAbsoluteUri.Should().BeFalse();
                result.OriginalString.Should().Be(Address);
            }

            [Fact]
            public void ShouldThrowForInvalidUris()
            {
                Action action = () => ReadValue("a:b", r => r.ReadUri());

                action.Should().Throw<FormatException>();
            }
        }

        private sealed class FakeValueReader : ValueReader
        {
            internal const string Position = "current position";

            internal string CurrentContents { get; set; }

            public override bool ReadBoolean()
            {
                throw new NotImplementedException();
            }

            public override char ReadChar()
            {
                throw new NotImplementedException();
            }

            public override bool ReadNull()
            {
                throw new NotImplementedException();
            }

            public override string ReadString()
            {
                return this.CurrentContents;
            }

            internal override string GetCurrentPosition()
            {
                return Position;
            }

            private protected override ReadOnlySpan<char> ReadTrimmedString()
            {
                return this.CurrentContents.AsSpan();
            }
        }
    }
}
