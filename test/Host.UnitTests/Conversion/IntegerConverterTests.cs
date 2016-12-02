namespace Host.UnitTests.Conversion
{
    using Crest.Host;
    using Crest.Host.Conversion;
    using NUnit.Framework;

    [TestFixture]
    public sealed class IntegerConverterTests
    {
        [TestCase("123", ExpectedResult = 123)]
        public int ParseIntegerValueShouldMatchValidIntegers(string integer)
        {
            var segment = new StringSegment("X" + integer + "X", 1, integer.Length + 1);

            long value;
            bool result = IntegerConverter.ParseIntegerValue(segment, out value);

            Assert.That(result, Is.True);
            return (int)value;
        }

        [TestCase("123a")]
        [TestCase("+123")]
        [TestCase("-123")]
        [TestCase("a123")]
        public void ParseIntegerValueShouldNotMatchInvalidIntegers(string integer)
        {
            var segment = new StringSegment(integer, 0, integer.Length);

            long value;
            bool result = IntegerConverter.ParseIntegerValue(segment, out value);

            Assert.That(result, Is.False);
            Assert.That(value, Is.EqualTo(0L));
        }

        [TestCase("123", ExpectedResult = 123)]
        [TestCase("+123", ExpectedResult = 123)]
        [TestCase("-123", ExpectedResult = -123)]
        public int ParseSignedValueShouldMatchValidIntegers(string integer)
        {
            var segment = new StringSegment("X" + integer + "X", 1, integer.Length + 1);

            long value;
            bool result = IntegerConverter.ParseSignedValue(segment, out value);

            Assert.That(result, Is.True);
            return (int)value;
        }

        [TestCase("123a")]
        [TestCase("a123")]
        public void ParseSignedValueShouldNotMatchInvalidIntegers(string integer)
        {
            var segment = new StringSegment(integer, 0, integer.Length);

            long value;
            bool result = IntegerConverter.ParseSignedValue(segment, out value);

            Assert.That(result, Is.False);
            Assert.That(value, Is.EqualTo(0L));
        }
    }
}
