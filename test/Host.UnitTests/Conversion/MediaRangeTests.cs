namespace Host.UnitTests.Conversion
{
    using System;
    using Crest.Host;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using Xunit;

    public class MediaRangeTests
    {
        public sealed class Parsing : MediaRangeTests
        {
            [Fact]
            public void ShouldAllowValidSymbolsInTheTypes()
            {
                // The symbols are taken from https://tools.ietf.org/html/rfc7230#section-3.2.6
                const string ValidCharacters = "!#$%&'*+-.^_`|~09azAZ";
                const string MediaType = ValidCharacters + "/" + ValidCharacters;

                var rangeA = new MediaRange(new StringSegment(MediaType, 0, MediaType.Length));
                var rangeB = new MediaRange(new StringSegment(MediaType, 0, MediaType.Length));

                rangeA.MediaTypesMatch(rangeB).Should().BeTrue();
            }

            [Theory]
            [InlineData("*/*;parameter=value ; q=0.8", 800)]
            [InlineData("*/* invalid", 1000)]
            [InlineData("*/* ; p=1;q=0.2;", 200)]
            [InlineData("*/*;p=1", 1000)]
            public void ShouldIgnoreParametersWhenParsingTheQuality(string input, int quality)
            {
                var range = new MediaRange(new StringSegment(input, 0, input.Length));

                range.Quality.Should().Be(quality);
            }

            [Theory]
            [InlineData("*/*", "text/plain", true)]
            [InlineData("text/*", "text/plain", true)]
            [InlineData("text/plain", "text/plain", true)]
            [InlineData("text/xml", "text/plain", false)]
            [InlineData("application/*", "text/plain", false)]
            public void ShouldMatchMediaTypes(string first, string second, bool expected)
            {
                var rangeA = new MediaRange(new StringSegment(first, 0, first.Length));
                var rangeB = new MediaRange(new StringSegment(second, 0, second.Length));

                bool result = rangeA.MediaTypesMatch(rangeB);

                // Shouldn't matter which way around they are...
                rangeB.MediaTypesMatch(rangeA).Should().Be(result);
                result.Should().Be(expected);
            }

            [Theory]
            [InlineData("0", 0)]
            [InlineData("0.", 0)]
            [InlineData("0.1", 100)]
            [InlineData("0.12", 120)]
            [InlineData("0.123", 123)]
            [InlineData("0.1239", 123)]
            [InlineData("0.1;2=3", 100)]
            [InlineData("1", 1000)]
            [InlineData("1.000", 1000)]
            public void ShouldParseTheQuality(string input, int expected)
            {
                string media = "*/*;q=" + input;
                var range = new MediaRange(new StringSegment(media, 0, media.Length));
                range.Quality.Should().Be(expected);
            }
        }

        public sealed class Constructor : MediaRangeTests
        {
            [Fact]
            public void ShouldThrowForInvalidQualityValues()
            {
                Action action = () => new MediaRange(new StringSegment("*/*;Q=2.0", 0, 9));

                action.ShouldThrow<ArgumentException>();
            }

            [Theory]
            [InlineData("missing_separator")]
            [InlineData("missing_sub_type/")]
            [InlineData("not_allowed / spaces_here")]
            [InlineData("")]
            [InlineData(" ")]
            public void ShouldThrowIfNotAValidMediaType(string input)
            {
                Action action = () => new MediaRange(new StringSegment(input, 0, input.Length));

                action.ShouldThrow<ArgumentException>();
            }
        }

        public sealed class Quality : MediaRangeTests
        {
            [Fact]
            public void ShouldDefualtTo1000()
            {
                var range = new MediaRange(new StringSegment("*/*", 0, 3));

                range.Quality.Should().Be(1000);
            }
        }
    }
}
