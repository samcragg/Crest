namespace Host.UnitTests.Conversion
{
    using Crest.Host;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using Xunit;

    public class IntegerConverterTests
    {
        public sealed class ParseIntegerValue : IntegerConverterTests
        {
            [Theory]
            [InlineData("123", 123)]
            public void ShouldMatchValidIntegers(string integer, int expected)
            {
                var segment = new StringSegment("X" + integer + "X", 1, integer.Length + 1);

                bool result = IntegerConverter.ParseIntegerValue(segment, out long value);

                result.Should().BeTrue();
                ((int)value).Should().Be(expected);
            }

            [Theory]
            [InlineData("123a")]
            [InlineData("+123")]
            [InlineData("-123")]
            [InlineData("a123")]
            public void ShouldNotMatchInvalidIntegers(string integer)
            {
                var segment = new StringSegment(integer, 0, integer.Length);

                bool result = IntegerConverter.ParseIntegerValue(segment, out long value);

                result.Should().BeFalse();
                value.Should().Be(0);
            }
        }

        public sealed class ParseSignedValue : IntegerConverterTests
        {
            [Theory]
            [InlineData("123", 123)]
            [InlineData("+123", 123)]
            [InlineData("-123", -123)]
            public void ShouldMatchValidIntegers(string integer, int expected)
            {
                var segment = new StringSegment("X" + integer + "X", 1, integer.Length + 1);

                bool result = IntegerConverter.ParseSignedValue(segment, out long value);

                result.Should().BeTrue();
                ((int)value).Should().Be(expected);
            }

            [Theory]
            [InlineData("123a")]
            [InlineData("a123")]
            public void ShouldNotMatchInvalidIntegers(string integer)
            {
                bool result = IntegerConverter.ParseSignedValue(
                    new StringSegment(integer),
                    out long value);

                result.Should().BeFalse();
                value.Should().Be(0);
            }
        }
    }
}
