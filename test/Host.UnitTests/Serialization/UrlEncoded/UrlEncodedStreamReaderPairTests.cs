namespace Host.UnitTests.Serialization.UrlEncoded
{
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class UrlEncodedStreamReaderPairTests
    {
        public sealed class CompareTo : UrlEncodedStreamReaderPairTests
        {
            [Theory]
            [InlineData("Array.1", "Array.2")]
            [InlineData("Array.2", "Array.10")]
            [InlineData("propertyA", "PropertyB")] // Note the casing
            [InlineData("Property", "Property.Other")]
            public void ShouldReturnNegativeIfThisInstancePrecedesTheKey(string key, string other)
            {
                var pair = new UrlEncodedStreamReader.Pair(key, "");

                int result = pair.CompareTo(new UrlEncodedStreamReader.Pair(other, ""));

                result.Should().BeNegative();
            }

            [Theory]
            [InlineData("Array.2", "Array.1")]
            [InlineData("Array.10", "Array.2")]
            [InlineData("PropertyB", "propertyA")] // Note the casing
            [InlineData("Abc", "Ab")]
            [InlineData("Property.Other", "Property")]
            public void ShouldReturnPositiveIfThisInstanceFollowsTheKey(string key, string other)
            {
                var pair = new UrlEncodedStreamReader.Pair(key, "");

                int result = pair.CompareTo(new UrlEncodedStreamReader.Pair(other, ""));

                result.Should().BePositive();
            }

            [Theory]
            [InlineData("Array.1", "Array.01")]
            [InlineData("propertyA", "PropertyA")]
            public void ShouldReturnZeroIfTheKeyIsTheSame(string key, string other)
            {
                var pair = new UrlEncodedStreamReader.Pair(key, "");

                int result = pair.CompareTo(new UrlEncodedStreamReader.Pair(other, ""));

                result.Should().Be(0);
            }
        }

        public sealed class GetArrayIndex : UrlEncodedStreamReaderPairTests
        {
            [Fact]
            public void ShouldReturnANegativeNumberIfThePartIndexIsInvalid()
            {
                var pair = new UrlEncodedStreamReader.Pair("1", "");

                int result = pair.GetArrrayIndex(2);

                result.Should().BeNegative();
            }

            [Fact]
            public void ShouldReturnTheIntegerAtTheSpecifiedIndex()
            {
                var pair = new UrlEncodedStreamReader.Pair("A.1", "");

                int result = pair.GetArrrayIndex(1);

                result.Should().Be(1);
            }
        }

        public sealed class GetPart : UrlEncodedStreamReaderPairTests
        {
            [Fact]
            public void ShouldReturnNullIfThePartDoesNotExist()
            {
                var pair = new UrlEncodedStreamReader.Pair("A", "");

                string result = pair.GetPart(2);

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnThePartAtTheSpecifiedIndex()
            {
                var pair = new UrlEncodedStreamReader.Pair("A.B", "");

                string result = pair.GetPart(1);

                result.Should().Be("B");
            }
        }

        public sealed class PartIsEqual : UrlEncodedStreamReaderPairTests
        {
            [Fact]
            public void ShouldReturnFalseIfThePartDoesNotExist()
            {
                var pair = new UrlEncodedStreamReader.Pair("one", "");
                var other = new UrlEncodedStreamReader.Pair("one.two", "");

                bool result = pair.PartIsEqual(other, 1);

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfThePartIsEqual()
            {
                var pair = new UrlEncodedStreamReader.Pair("one", "");
                var other = new UrlEncodedStreamReader.Pair("one.two", "");

                bool result = pair.PartIsEqual(other, 0);

                result.Should().BeTrue();
            }
        }
    }
}
