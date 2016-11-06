namespace Host.UnitTests
{
    using System;
    using System.Linq;
    using Crest.Host;
    using NUnit.Framework;

    [TestFixture]
    public sealed class QueryLookupTests
    {
        [Test]
        public void CountShouldReturnTheNumberOfUniqueKeys()
        {
            var lookup = new QueryLookup("?key1=_&key2=_&key1=_");

            Assert.That(lookup.Count, Is.EqualTo(2));
        }

        [Test]
        public void ContainsShouldReturnTrueIfTheKeyIsPresent()
        {
            var lookup = new QueryLookup("key=value");

            Assert.That(lookup.Contains("key"), Is.True);
        }

        [Test]
        public void ContainsShouldReturnFalseIfTheKeyIsNotPresent()
        {
            var lookup = new QueryLookup("?key=value");

            Assert.That(lookup.Contains("unknown"), Is.False);
        }

        [Test]
        public void IndexShouldReturnAllTheValues()
        {
            var lookup = new QueryLookup("?key=1&key=2");

            Assert.That(lookup["key"].ToList(), Is.EqualTo(new[] { "1", "2" }));
        }

        [Test]
        public void IndexShouldReturnAnEmptySequenceIfTheKeyDoesNotExist()
        {
            var lookup = new QueryLookup("?key=value");

            Assert.That(lookup["unknown"], Is.Empty);
        }

        [Test]
        public void ShouldHandleEmptyQueryStrings()
        {
            var lookup = new QueryLookup(string.Empty);

            Assert.That(lookup.Count, Is.EqualTo(0));
        }

        [Test]
        public void ShouldHandleKeysWithoutValues()
        {
            var lookup = new QueryLookup("?key");

            Assert.That(lookup.Contains("key"), Is.True);
        }

        [Test]
        public void ShouldUnescapePrecentageEscapedKeys()
        {
            var lookup = new QueryLookup("?%41");

            Assert.That(lookup.Contains("A"), Is.True);
        }

        [Test]
        public void ShouldIgnoreTheCaseOfPercentageEscapedValues()
        {
            var lookup = new QueryLookup("?%2A=%2a");

            Assert.That(lookup["*"].Single(), Is.EqualTo("*"));
        }

        [Test]
        public void ShouldUnescapeSpacesEncodedAsPlus()
        {
            var lookup = new QueryLookup("?a+b=c+d");

            Assert.That(lookup["a b"].Single(), Is.EqualTo("c d"));
        }

        [TestCase("%1")]
        [TestCase("%0G")]
        public void ShouldThrowUriFormatExceptionForInvalidPercentageEscapedValues(string value)
        {
            Assert.That(
                () => new QueryLookup("?" + value),
                Throws.InstanceOf<UriFormatException>());
        }
    }
}
