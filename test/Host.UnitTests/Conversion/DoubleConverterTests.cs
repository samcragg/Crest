namespace Host.UnitTests.Conversion
{
    using System;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using Xunit;

    public class DoubleConverterTests
    {
        public sealed class TryReadDouble
        {
            [Theory]
            [InlineData("infinity", double.PositiveInfinity)]
            [InlineData("+inf", double.PositiveInfinity)]
            [InlineData("+infinity", double.PositiveInfinity)]
            [InlineData("-INF", double.NegativeInfinity)]
            [InlineData("-INFINITY", double.NegativeInfinity)]
            public void ShouldParseInfinity(string value, double expected)
            {
                ParseResult<double> result = DoubleConverter.TryReadDouble(value.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Length.Should().Be(value.Length);
                result.Value.Should().Be(expected);
            }

            [Theory]
            [InlineData("+0", 0.0)]
            [InlineData("+4.9406564584124654E-324", double.Epsilon)]
            [InlineData("+1.7976931348623157E+308", double.MaxValue)]
            [InlineData("-0", -0.0)]
            [InlineData("-4.9406564584124654E-324", -double.Epsilon)]
            [InlineData("-1.7976931348623157E+308", double.MinValue)]
            [InlineData("+1797693134862315700E+290", double.MaxValue)]
            public void ShouldParseMaximumRanges(string value, double expected)
            {
                ParseResult<double> result = DoubleConverter.TryReadDouble(value.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Length.Should().Be(value.Length);
                result.Value.Should().Be(expected);
            }

            [Fact]
            public void ShouldParseNaN()
            {
                ParseResult<double> result = DoubleConverter.TryReadDouble("NaN".AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Length.Should().Be(3);
                double.IsNaN(result.Value).Should().BeTrue();
            }

            [Theory]
            [InlineData("1", 1.0)]
            [InlineData(".2", 0.2)]
            [InlineData("1e1", 10.0)]
            [InlineData(".2e1", 2.0)]
            [InlineData("1.2e1", 12.0)]
            [InlineData("+1", 1.0)]
            [InlineData("+.2", 0.2)]
            [InlineData("+1e1", 10.0)]
            [InlineData("+.2e1", 2.0)]
            [InlineData("+1.2e1", 12.0)]
            [InlineData("+1e+1", 10.0)]
            [InlineData("+.2e+1", 2.0)]
            [InlineData("+1.2e+1", 12.0)]
            [InlineData("010.0", 10.0)]
            [InlineData("0.010", 0.01)]
            [InlineData("10.01", 10.01)]
            public void ShouldParseValidFormats(string value, double expected)
            {
                ParseResult<double> result = DoubleConverter.TryReadDouble(value.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Length.Should().Be(value.Length);
                result.Value.Should().Be(expected);
            }

            [Theory]
            [InlineData("1e", 1)]
            [InlineData("1e.0", 1)]
            [InlineData("1.e", 2)]
            [InlineData("1e++0", 1)]
            [InlineData("1ee0", 1)]
            public void ShouldReadAsMuchAsPossible(string value, int length)
            {
                ParseResult<double> result = DoubleConverter.TryReadDouble(value.AsSpan());

                result.Length.Should().Be(length);
                result.Value.Should().Be(1);
            }

            [Theory]
            [InlineData(".")]
            [InlineData("++0")]
            [InlineData("+")]
            public void ShouldReturnAnErrorForInvalidFormats(string value)
            {
                ParseResult<double> result = DoubleConverter.TryReadDouble(value.AsSpan());

                result.Error.Should().MatchEquivalentOf("*format*");
            }

            [Theory]
            [InlineData("+1E+309", double.PositiveInfinity)]
            [InlineData("+1E+3000", double.PositiveInfinity)]
            [InlineData("-1E+309", double.NegativeInfinity)]
            [InlineData("-1E+3000", double.NegativeInfinity)]
            public void ShouldSilentlyOverflow(string value, double expected)
            {
                ParseResult<double> result = DoubleConverter.TryReadDouble(value.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Length.Should().Be(value.Length);
                result.Value.Should().Be(expected);
            }

            [Theory]
            [InlineData("+1E-325")]
            [InlineData("-1E-325")]
            public void ShouldSilentlyUnderflow(string value)
            {
                ParseResult<double> result = DoubleConverter.TryReadDouble(value.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Length.Should().Be(value.Length);
                result.Value.Should().Be(0);
            }

            [Theory]
            [InlineData("123.456789012345678")]
            [InlineData("123456789012345678")]
            [InlineData("123456789012345678.901")]
            [InlineData("-123.456789012345678")]
            [InlineData("-123456789012345678")]
            [InlineData("-123456789012345678.901")]
            public void ShouldTruncateInsignificantDigits(string value)
            {
                ParseResult<double> result = DoubleConverter.TryReadDouble(value.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Length.Should().Be(value.Length);
                result.Value.Should().Be(double.Parse(value));
            }
        }
    }
}
