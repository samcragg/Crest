namespace Host.UnitTests.Conversion
{
    using System;
    using System.Globalization;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using Xunit;

    public class DecimalConverterTests
    {
        public sealed class TryReadDecimal : DecimalConverterTests
        {
            [Theory]
            [InlineData("1", "1")]
            [InlineData(".2", "0.2")]
            [InlineData("1e1", "10")]
            [InlineData(".2e1", "2")]
            [InlineData("1.2e1", "12")]
            [InlineData("+1", "1")]
            [InlineData("+.2", "0.2")]
            [InlineData("+1e1", "10")]
            [InlineData("+.2e1", "2")]
            [InlineData("+1.2e1", "12")]
            [InlineData("+1e+1", "10")]
            [InlineData("+.2e+1", "2")]
            [InlineData("+1.2e+1", "12")]
            public void ShouldParseValidFormats(string value, string expected)
            {
                ParseResult<decimal> result = DecimalConverter.TryReadDecimal(value.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Length.Should().Be(value.Length);
                result.Value.ToString(NumberFormatInfo.InvariantInfo)
                      .Should().Be(expected);
            }

            [Theory]
            [InlineData("1e", 1)]
            [InlineData("1e.0", 1)]
            [InlineData("1.e", 2)]
            [InlineData("1e++0", 1)]
            [InlineData("1ee0", 1)]
            public void ShouldReadAsMuchAsPossible(string value, int length)
            {
                ParseResult<decimal> result =
                    DecimalConverter.TryReadDecimal(value.AsSpan());

                result.Length.Should().Be(length);
                result.Value.Should().Be(1);
            }

            [Theory]
            [InlineData("-79228162514264337593543950335")]
            [InlineData("0")]
            [InlineData("79228162514264337593543950335")]
            public void ShouldReadDecimalLimits(string value)
            {
                ParseResult<decimal> result =
                    DecimalConverter.TryReadDecimal(value.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Value.ToString(NumberFormatInfo.InvariantInfo)
                      .Should().Be(value);
            }

            [Theory]
            [InlineData(".")]
            [InlineData("++0")]
            [InlineData("+")]
            public void ShouldReturnAnErrorForInvalidFormats(string value)
            {
                ParseResult<decimal> result =
                    DecimalConverter.TryReadDecimal(value.AsSpan());

                result.Error.Should().MatchEquivalentOf("*format*");
            }

            [Theory]
            [InlineData("+79228162514264337593543950336")]
            [InlineData("-79228162514264337593543950336")]
            [InlineData("+792281625142643375935439503350")]
            [InlineData("+1e30")]
            [InlineData("+7.93e29")]
            public void ShouldReturnAnErrorForValueOutsideOfTheDecimalRange(string value)
            {
                ParseResult<decimal> result =
                    DecimalConverter.TryReadDecimal(value.AsSpan());

                result.Error.Should().MatchEquivalentOf("*range*");
            }

            [Theory]
            [InlineData("123.000000000000000000000000001", "123.00000000000000000000000000")]
            [InlineData("123.000000000000000000000000005", "123.00000000000000000000000001")]
            [InlineData("79228162495817593519834398719.5", "79228162495817593519834398720")]
            [InlineData("7922816251426433759354395033.54", "7922816251426433759354395033.5")]
            [InlineData("7922816251426433759354395033.549", "7922816251426433759354395033.5")]
            [InlineData("7922816251426433759354395033.55", "7922816251426433759354395034")]
            [InlineData("7922816251426433759354395035.5", "7922816251426433759354395036")]
            [InlineData("9999999999999999999999999999.9", "10000000000000000000000000000")]
            public void ShouldRoundDecimalPlaces(string value, string expected)
            {
                ParseResult<decimal> result =
                    DecimalConverter.TryReadDecimal(value.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Value.ToString(NumberFormatInfo.InvariantInfo)
                      .Should().Be(expected);
            }
        }
    }
}
