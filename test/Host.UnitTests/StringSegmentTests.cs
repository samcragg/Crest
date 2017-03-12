namespace Host.UnitTests
{
    using System;
    using System.Collections;
    using System.Linq;
    using Crest.Host;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class StringSegmentTests
    {
        [TestFixture]
        public sealed class Constructor : StringSegmentTests
        {
            [Test]
            public void ShouldSetTheProperties()
            {
                const string ExampleString = "Example";

                var segment = new StringSegment(ExampleString, 1, 5);

                segment.String.Should().Be(ExampleString);
                segment.Start.Should().Be(1);
                segment.End.Should().Be(5);
            }
        }

        [TestFixture]
        public sealed class Count : StringSegmentTests
        {
            [Test]
            public void ShouldReturnTheNumberOfCharactersOfTheSubString()
            {
                var segment = new StringSegment("string", 2, 3);

                segment.Count.Should().Be(1);
            }
        }

        [TestFixture]
        public sealed new class Equals : StringSegmentTests
        {
            [Test]
            public void ShouldReturnFalseForNullValues()
            {
                var segment = new StringSegment("012", 0, 3);

                bool result = segment.Equals(null, StringComparison.Ordinal);

                result.Should().BeFalse();
            }

            [Test]
            public void ShouldReturnFalseIfTheSubStringIsNotEqual()
            {
                var segment = new StringSegment("0123456", 2, 5);

                bool result = segment.Equals("345", StringComparison.Ordinal);

                result.Should().BeFalse();
            }

            [Test]
            public void ShouldReturnFalseIfTheValueIsLongerThanTheSubstring()
            {
                var segment = new StringSegment("0123456", 2, 4);

                bool result = segment.Equals("234", StringComparison.Ordinal);

                result.Should().BeFalse();
            }

            [Test]
            public void ShouldReturnTrueIfTheSubStringIsEqual()
            {
                var segment = new StringSegment("0123456", 2, 5);

                bool result = segment.Equals("234", StringComparison.Ordinal);

                result.Should().BeTrue();
            }

            [Test]
            public void ShouldUseTheComparisonType()
            {
                var segment = new StringSegment("abc", 0, 3);

                segment.Equals("AbC", StringComparison.Ordinal)
                       .Should().BeFalse();

                segment.Equals("AbC", StringComparison.OrdinalIgnoreCase)
                       .Should().BeTrue();
            }
        }

        [TestFixture]
        public sealed class GetEnumerator : StringSegmentTests
        {
            [Test]
            public void ShouldReturnAllOfTheSubString()
            {
                var segment = new StringSegment("0123456", 2, 4);

                string subString = new string(Enumerable.ToArray(segment));

                subString.Should().Be("23");
            }
        }

        [TestFixture]
        public sealed class Index : StringSegmentTests
        {
            [Test]
            public void ShouldReturnTheCharacterRelativeToTheStartOfTheSubString()
            {
                var segment = new StringSegment("0123456", 2, 4);

                segment[0].Should().Be('2');
                segment[1].Should().Be('3');
            }
        }

        [TestFixture]
        public sealed class NonGenericGetEnumerator : StringSegmentTests
        {
            [Test]
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

        [TestFixture]
        public sealed new class ToString : StringSegmentTests
        {
            [Test]
            public void ShouldReturnAllOfTheSubString()
            {
                var segment = new StringSegment("0123456", 2, 4);

                string subString = segment.ToString();

                subString.Should().Be("23");
            }
        }
    }
}
