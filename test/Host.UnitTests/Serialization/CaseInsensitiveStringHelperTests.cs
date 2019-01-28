namespace Host.UnitTests.Serialization
{
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class CaseInsensitiveStringHelperTests
    {
        public new sealed class Equals : CaseInsensitiveStringHelperTests
        {
            [Fact]
            public void ShouldReturnFalseIfTwoStringsAreNotEqual()
            {
                bool result = CaseInsensitiveStringHelper.Equals("ONE", "TWO");

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseIfTwoStringsHaveADifferentLength()
            {
                bool result = CaseInsensitiveStringHelper.Equals("FIRST", "SECOND");

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfTwoStringsAreEqualButDifferByCase()
            {
                bool result = CaseInsensitiveStringHelper.Equals("VALUE", "value");

                result.Should().BeTrue();
            }
        }

        public new sealed class GetHashCode : CaseInsensitiveStringHelperTests
        {
            [Fact]
            public void ShouldReturnDifferentHashCodesForDifferentStrings()
            {
                // GetHashCode could return the same hash code for all values
                // and still be valid, though not useful. The way the algorithm
                // is implemented we know for values of the same length it will
                // produce different results
                int first = CaseInsensitiveStringHelper.GetHashCode("one");
                int second = CaseInsensitiveStringHelper.GetHashCode("two");

                first.Should().NotBe(second);
            }

            [Fact]
            public void ShouldReturnTheSameHashCodeForStringsThatHaveDifferentCasing()
            {
                int lowercase = CaseInsensitiveStringHelper.GetHashCode("value");
                int uppercase = CaseInsensitiveStringHelper.GetHashCode("VALUE");

                lowercase.Should().Be(uppercase);
            }
        }
    }
}
