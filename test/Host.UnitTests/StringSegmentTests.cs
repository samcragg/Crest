namespace Host.UnitTests
{
    using System;
    using System.Collections;
    using System.Linq;
    using Crest.Host;
    using FluentAssertions;
    using Xunit;

    public class StringSegmentTests
    {
        public sealed class Constructor : StringSegmentTests
        {
            [Fact]
            public void ShouldSetTheProperties()
            {
                const string ExampleString = "Example";

                var segment = new StringSegment(ExampleString, 1, 5);

                segment.String.Should().Be(ExampleString);
                segment.Start.Should().Be(1);
                segment.End.Should().Be(5);
            }
        }

        public sealed class Count : StringSegmentTests
        {
            [Fact]
            public void ShouldReturnTheNumberOfCharactersOfTheSubString()
            {
                var segment = new StringSegment("string", 2, 3);

                segment.Count.Should().Be(1);
            }
        }

        public sealed new class Equals : StringSegmentTests
        {
            [Fact]
            public void ShouldReturnFalseForNullValues()
            {
                var segment = new StringSegment("012", 0, 3);

                bool result = segment.Equals(null, StringComparison.Ordinal);

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseIfTheSubStringIsNotEqual()
            {
                var segment = new StringSegment("0123456", 2, 5);

                bool result = segment.Equals("345", StringComparison.Ordinal);

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseIfTheValueIsLongerThanTheSubstring()
            {
                var segment = new StringSegment("0123456", 2, 4);

                bool result = segment.Equals("234", StringComparison.Ordinal);

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfTheSubStringIsEqual()
            {
                var segment = new StringSegment("0123456", 2, 5);

                bool result = segment.Equals("234", StringComparison.Ordinal);

                result.Should().BeTrue();
            }

            [Fact]
            public void ShouldUseTheComparisonType()
            {
                var segment = new StringSegment("abc", 0, 3);

                segment.Equals("AbC", StringComparison.Ordinal)
                       .Should().BeFalse();

                segment.Equals("AbC", StringComparison.OrdinalIgnoreCase)
                       .Should().BeTrue();
            }
        }

        public sealed class GetEnumerator : StringSegmentTests
        {
            [Fact]
            public void ShouldReturnAllOfTheSubString()
            {
                var segment = new StringSegment("0123456", 2, 4);

                string subString = new string(Enumerable.ToArray(segment));

                subString.Should().Be("23");
            }
        }

        public sealed class Index : StringSegmentTests
        {
            [Fact]
            public void ShouldReturnTheCharacterRelativeToTheStartOfTheSubString()
            {
                var segment = new StringSegment("0123456", 2, 4);

                segment[0].Should().Be('2');
                segment[1].Should().Be('3');
            }
        }

        public sealed class NonGenericGetEnumerator : StringSegmentTests
        {
            [Fact]
            public void ShouldReturnAllOfTheSubString()
            {
                IEnumerable segment = new StringSegment("0123456", 2, 4);

                IEnumerator enumerator = segment.GetEnumerator();

                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Should().Be('2');

                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Should().Be('3');

                enumerator.MoveNext().Should().BeFalse();
            }
        }

        public sealed new class ToString : StringSegmentTests
        {
            [Fact]
            public void ShouldReturnAllOfTheSubString()
            {
                var segment = new StringSegment("0123456", 2, 4);

                string subString = segment.ToString();

                subString.Should().Be("23");
            }
        }
    }
}
