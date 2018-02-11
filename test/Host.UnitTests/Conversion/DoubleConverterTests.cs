namespace Host.UnitTests.Conversion
{
    using System;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using Xunit;

    public class DoubleConverterTests
    {
        public sealed class StringToDoubleTest
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
        }
    }
}
