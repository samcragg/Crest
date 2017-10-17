namespace Host.UnitTests.Serialization
{
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class IntegerConverterTests
    {
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
