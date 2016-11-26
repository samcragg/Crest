namespace Host.UnitTests.Conversion
{
    using System;
    using Crest.Host;
    using Crest.Host.Conversion;
    using NUnit.Framework;

    [TestFixture]
    public sealed class MediaRangeTests
    {
        [Test]
        public void QualityShouldDefualtTo1000()
        {
            var range = new MediaRange(new StringSegment("*/*", 0, 3));

            Assert.That(range.Quality, Is.EqualTo(1000));
        }

        [TestCase("missing_separator")]
        [TestCase("missing_sub_type/")]
        [TestCase("not_allowed / spaces_here")]
        [TestCase("")]
        [TestCase(" ")]
        public void ShouldThrowIfNotAValidMediaType(string input)
        {
            Assert.That(
                () => new MediaRange(new StringSegment(input, 0, input.Length)),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void ShouldAllowValidSymbolsInTheTypes()
        {
            // The symbols are taken from https://tools.ietf.org/html/rfc7230#section-3.2.6
            const string ValidCharacters = "!#$%&'*+-.^_`|~09azAZ";
            const string MediaType = ValidCharacters + "/" + ValidCharacters;

            var rangeA = new MediaRange(new StringSegment(MediaType, 0, MediaType.Length));
            var rangeB = new MediaRange(new StringSegment(MediaType, 0, MediaType.Length));

            Assert.That(rangeA.MediaTypesMatch(rangeB), Is.True);
        }

        [TestCase("0", ExpectedResult = 0)]
        [TestCase("0.", ExpectedResult = 0)]
        [TestCase("0.1", ExpectedResult = 100)]
        [TestCase("0.12", ExpectedResult = 120)]
        [TestCase("0.123", ExpectedResult = 123)]
        [TestCase("0.1239", ExpectedResult = 123)]
        [TestCase("0.1;2=3", ExpectedResult = 100)]
        [TestCase("1", ExpectedResult = 1000)]
        [TestCase("1.000", ExpectedResult = 1000)]
        public int ShouldParseTheQuality(string input)
        {
            string media = "*/*;q=" + input;
            var range = new MediaRange(new StringSegment(media, 0, media.Length));
            return range.Quality;
        }

        [TestCase("*/*;parameter=value ; q=0.8", 800)]
        [TestCase("*/* invalid", 1000)]
        [TestCase("*/* ; p=1;q=0.2;", 200)]
        [TestCase("*/*;p=1", 1000)]
        public void ShouldIgnoreParametersWhenParsingTheQuality(string input, int quality)
        {
            var range = new MediaRange(new StringSegment(input, 0, input.Length));

            Assert.That(range.Quality, Is.EqualTo(quality));
        }

        [Test]
        public void ShouldThrowForInvalidQualityValues()
        {
            Assert.That(
                () => new MediaRange(new StringSegment("*/*;Q=2.0", 0, 9)),
                Throws.InstanceOf<ArgumentException>());
        }

        [TestCase("*/*", "text/plain", ExpectedResult = true)]
        [TestCase("text/*", "text/plain", ExpectedResult = true)]
        [TestCase("text/plain", "text/plain", ExpectedResult = true)]
        [TestCase("text/xml", "text/plain", ExpectedResult = false)]
        [TestCase("application/*", "text/plain", ExpectedResult = false)]
        public bool ShouldMatchMediaTypes(string first, string second)
        {
            var rangeA = new MediaRange(new StringSegment(first, 0, first.Length));
            var rangeB = new MediaRange(new StringSegment(second, 0, second.Length));

            bool result = rangeA.MediaTypesMatch(rangeB);

            // Shouldn't matter which way around they are...
            Assert.That(rangeB.MediaTypesMatch(rangeA), Is.EqualTo(result));
            return result;
        }
    }
}
