namespace Host.UnitTests.Conversion
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using Xunit;

    public class IntegerConverterTests
    {
        public sealed class ReadUnsignedInt : IntegerConverterTests
        {
            [Fact]
            public void ShouldAllowLeadingZeros()
            {
                ParseResult<ulong> result = IntegerConverter.TryReadUnsignedInt(
                    "001".AsSpan(),
                    ulong.MaxValue);

                result.Length.Should().Be(3);
                result.Value.Should().Be(1);
            }

            [Theory]
            [InlineData("18446744073709551616")]
            public void ShouldCheckForOverflow(string value)
            {
                ParseResult<ulong> result = IntegerConverter.TryReadUnsignedInt(
                    value.AsSpan(),
                    ulong.MaxValue);

                result.Error.Should().ContainEquivalentOf("range");
            }

            [Fact]
            public void ShouldErrorWithOutOfRangeIfGreaterThanTheMaximumValue()
            {
                ParseResult<ulong> result = IntegerConverter.TryReadUnsignedInt(
                    "101".AsSpan(),
                    100);

                result.Error.Should().ContainEquivalentOf("range");
            }

            [Theory]
            [InlineData(0)]
            [InlineData(ulong.MaxValue)]
            public void ShouldReadIntegerLimits(ulong value)
            {
                ParseResult<ulong> result = IntegerConverter.TryReadUnsignedInt(
                    value.ToString(NumberFormatInfo.InvariantInfo).AsSpan(),
                    ulong.MaxValue);

                result.Value.Should().Be(value);
            }

            [Fact]
            public void ShouldReturnAnErrorIfNotADigit()
            {
                ParseResult<ulong> result = IntegerConverter.TryReadUnsignedInt(
                    "a12".AsSpan(),
                    ulong.MaxValue);

                result.Error.Should().ContainEquivalentOf("digit");
            }
        }

        public sealed class TryReadSignedInt : IntegerConverterTests
        {
            [Fact]
            public void ShouldAllowLeadingZeros()
            {
                ParseResult<long> result = IntegerConverter.TryReadSignedInt(
                    "001".AsSpan(),
                    long.MinValue,
                    long.MaxValue);

                result.Length.Should().Be(3);
                result.Value.Should().Be(1);
            }

            [Theory]
            [InlineData("-9223372036854775809")]
            [InlineData("9223372036854775808")]
            public void ShouldCheckForOverflow(string value)
            {
                ParseResult<long> result = IntegerConverter.TryReadSignedInt(
                    value.AsSpan(),
                    long.MinValue,
                    long.MaxValue);

                result.Error.Should().ContainEquivalentOf("range");
            }

            [Theory]
            [InlineData(long.MinValue)]
            [InlineData(0)]
            [InlineData(long.MaxValue)]
            public void ShouldReadIntegerLimits(long value)
            {
                ParseResult<long> result = IntegerConverter.TryReadSignedInt(
                    value.ToString(NumberFormatInfo.InvariantInfo).AsSpan(),
                    long.MinValue,
                    long.MaxValue);

                result.Value.Should().Be(value);
            }

            [Fact]
            public void ShouldReturnAnErrorIfNotANumber()
            {
                ParseResult<long> result = IntegerConverter.TryReadSignedInt(
                    "a12".AsSpan(),
                    long.MinValue,
                    long.MaxValue);

                result.Error.Should().ContainEquivalentOf("digit");
            }
        }

        public sealed class WriteInt64 : IntegerConverterTests
        {
            [Fact]
            public void ShouldWriteAtTheSpecifiedOffset()
            {
                byte[] buffer = new byte[IntegerConverter.MaximumTextLength + 1];

                IntegerConverter.WriteInt64(buffer, 1, -123);

                buffer.Should().StartWith(new byte[] { 0, (byte)'-' });
            }

            [Theory]
            [InlineData(long.MinValue)]
            [InlineData(0)]
            [InlineData(long.MaxValue)]
            public void ShouldWriteIntegerLimits(long value)
            {
                byte[] buffer = new byte[IntegerConverter.MaximumTextLength];
                string expected = value.ToString(NumberFormatInfo.InvariantInfo);

                int length = IntegerConverter.WriteInt64(buffer, 0, value);

                buffer.Take(length).Should().Equal(Encoding.UTF8.GetBytes(expected));
            }

            [Fact]
            public void ShouldWriteNumbersUpTo100()
            {
                byte[] buffer = new byte[2];

                // Since we write the digits in pairs, we need to test the first
                // 100 numbers are the correct pairs
                for (long i = 0; i < 100; i++)
                {
                    string expected = i.ToString(NumberFormatInfo.InvariantInfo);

                    int length = IntegerConverter.WriteInt64(buffer, 0, i);

                    buffer.Take(length).Should().Equal(Encoding.UTF8.GetBytes(expected));
                }
            }
        }

        public sealed class WriteUInt64 : IntegerConverterTests
        {
            [Fact]
            public void ShouldWriteAtTheSpecifiedOffset()
            {
                byte[] buffer = new byte[IntegerConverter.MaximumTextLength + 1];

                IntegerConverter.WriteUInt64(buffer, 1, 123);

                buffer.Should().StartWith(new byte[] { 0, (byte)'1' });
            }

            [Theory]
            [InlineData(0)]
            [InlineData(ulong.MaxValue)]
            public void ShouldWriteIntegerLimits(ulong value)
            {
                byte[] buffer = new byte[IntegerConverter.MaximumTextLength];
                string expected = value.ToString(NumberFormatInfo.InvariantInfo);

                int length = IntegerConverter.WriteUInt64(buffer, 0, value);

                buffer.Take(length).Should().Equal(Encoding.UTF8.GetBytes(expected));
            }

            [Fact]
            public void ShouldWriteNumbersUpTo100()
            {
                byte[] buffer = new byte[2];

                // Since we write the digits in pairs, we need to test the first
                // 100 numbers are the correct pairs
                for (ulong i = 0; i < 100; i++)
                {
                    string expected = i.ToString(NumberFormatInfo.InvariantInfo);

                    int length = IntegerConverter.WriteUInt64(buffer, 0, i);

                    buffer.Take(length).Should().Equal(Encoding.UTF8.GetBytes(expected));
                }
            }
        }
    }
}
