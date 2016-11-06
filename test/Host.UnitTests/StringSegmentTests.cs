namespace Host.UnitTests
{
    using System;
    using System.Collections;
    using System.Linq;
    using Crest.Host;
    using NUnit.Framework;

    [TestFixture]
    public sealed class StringSegmentTests
    {
        [Test]
        public void TheConstructorShouldSetTheProperties()
        {
            const string ExampleString = "Example";

            var segment = new StringSegment(ExampleString, 1, 5);

            Assert.That(segment.String, Is.EqualTo(ExampleString));
            Assert.That(segment.Start, Is.EqualTo(1));
            Assert.That(segment.End, Is.EqualTo(5));
        }

        [Test]
        public void CountShouldReturnTheNumberOfCharactersOfTheSubString()
        {
            var segment = new StringSegment("string", 2, 3);

            Assert.That(segment.Count, Is.EqualTo(1));
        }

        [Test]
        public void IndexShouldReturnTheCharacterRelativeToTheStartOfTheSubString()
        {
            var segment = new StringSegment("0123456", 2, 4);

            Assert.That(segment[0], Is.EqualTo('2'));
            Assert.That(segment[1], Is.EqualTo('3'));
        }

        [Test]
        public void EqualsShouldReturnTrueIfTheSubStringIsEqual()
        {
            var segment = new StringSegment("0123456", 2, 5);

            bool result = segment.Equals("234", StringComparison.Ordinal);

            Assert.That(result, Is.True);
        }

        [Test]
        public void EqualsShouldReturnFalseIfTheSubStringIsNotEqual()
        {
            var segment = new StringSegment("0123456", 2, 5);

            bool result = segment.Equals("345", StringComparison.Ordinal);

            Assert.That(result, Is.False);
        }

        [Test]
        public void EqualsShouldReturnFalseForNullValues()
        {
            var segment = new StringSegment("012", 0, 3);

            bool result = segment.Equals(null, StringComparison.Ordinal);

            Assert.That(result, Is.False);
        }

        [Test]
        public void EqualsShouldReturnFalseIfTheValueIsLongerThanTheSubstring()
        {
            var segment = new StringSegment("0123456", 2, 4);

            bool result = segment.Equals("234", StringComparison.Ordinal);

            Assert.That(result, Is.False);
        }

        [Test]
        public void EqualsShouldUseTheComparisonType()
        {
            var segment = new StringSegment("abc", 0, 3);

            Assert.That(segment.Equals("AbC", StringComparison.Ordinal), Is.False);
            Assert.That(segment.Equals("AbC", StringComparison.OrdinalIgnoreCase), Is.True);
        }

        [Test]
        public void GetEnumeratorShouldReturnAllOfTheSubString()
        {
            var segment = new StringSegment("0123456", 2, 4);

            string subString = new string(Enumerable.ToArray(segment));

            Assert.That(subString, Is.EqualTo("23"));
        }

        [Test]
        public void NonGenericGetEnumeratorShouldReturnAllOfTheSubString()
        {
            IEnumerable segment = new StringSegment("0123456", 2, 4);

            IEnumerator enumerator = segment.GetEnumerator();

            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo('2'));

            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo('3'));

            Assert.That(enumerator.MoveNext(), Is.False);
        }

        [Test]
        public void ToStringShouldReturnAllOfTheSubString()
        {
            var segment = new StringSegment("0123456", 2, 4);

            string subString = segment.ToString();

            Assert.That(subString, Is.EqualTo("23"));
        }
    }
}
