namespace Host.UnitTests.Conversion
{
    using Crest.Host;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class IntegerConverterTests
    {
        [TestFixture]
        public sealed class ParseIntegerValue : IntegerConverterTests
        {
            [TestCase("123", ExpectedResult = 123)]
            public int ShouldMatchValidIntegers(string integer)
            {
                var segment = new StringSegment("X" + integer + "X", 1, integer.Length + 1);

                long value;
                bool result = IntegerConverter.ParseIntegerValue(segment, out value);

                result.Should().BeTrue();
                return (int)value;
            }

            [TestCase("123a")]
            [TestCase("+123")]
            [TestCase("-123")]
            [TestCase("a123")]
            public void ShouldNotMatchInvalidIntegers(string integer)
            {
                var segment = new StringSegment(integer, 0, integer.Length);

                long value;
                bool result = IntegerConverter.ParseIntegerValue(segment, out value);

                result.Should().BeFalse();
                value.Should().Be(0);
            }
        }

        [TestFixture]
        public sealed class ParseSignedValue : IntegerConverterTests
        {
            [TestCase("123", ExpectedResult = 123)]
            [TestCase("+123", ExpectedResult = 123)]
            [TestCase("-123", ExpectedResult = -123)]
            public int ShouldMatchValidIntegers(string integer)
            {
                var segment = new StringSegment("X" + integer + "X", 1, integer.Length + 1);

                long value;
                bool result = IntegerConverter.ParseSignedValue(segment, out value);

                result.Should().BeTrue();
                return (int)value;
            }

            [TestCase("123a")]
            [TestCase("a123")]
            public void ShouldNotMatchInvalidIntegers(string integer)
            {
                var segment = new StringSegment(integer, 0, integer.Length);

                long value;
                bool result = IntegerConverter.ParseSignedValue(segment, out value);

                result.Should().BeFalse();
                value.Should().Be(0);
            }
        }
    }
}
