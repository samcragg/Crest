namespace OpenApi.UnitTests
{
    using System;
    using Crest.OpenApi;
    using FluentAssertions;
    using Xunit;

    public class StringVersionComparerTests
    {
        private readonly StringVersionComparer comparer = new StringVersionComparer();

        public sealed class Compare : StringVersionComparerTests
        {
            [Fact]
            public void ShouldIgnoreTheCaseOfTheLeadingCharacter()
            {
                int result = this.comparer.Compare("v1", "V1");

                result.Should().Be(0);
            }

            [Fact]
            public void ShouldReturnANegativeNumberIfOnlyXIsNull()
            {
                int result = this.comparer.Compare(null, string.Empty);

                result.Should().BeNegative();
            }

            [Fact]
            public void ShouldReturnAPositiveNumberIfOnlyYIsNull()
            {
                int result = this.comparer.Compare(string.Empty, null);

                result.Should().BePositive();
            }

            [Theory]
            [InlineData("v2", "v10", -1)]
            [InlineData("v10", "v2", 1)]
            [InlineData("v2", "v3", -1)]
            [InlineData("v3", "v2", 1)]
            public void ShouldReturnTheExpectedSign(string x, string y, int sign)
            {
                int result = this.comparer.Compare(x, y);

                Math.Sign(result).Should().Be(sign);
            }

            [Fact]
            public void ShouldReturnZeroForEqualStrings()
            {
                // Create two strings with different references
                int result = this.comparer.Compare("v1", "v" + 1);

                result.Should().Be(0);
            }

            [Fact]
            public void ShouldReturnZeroForTwoNullStrings()
            {
                int result = this.comparer.Compare(null, null);

                result.Should().Be(0);
            }
        }
    }
}
