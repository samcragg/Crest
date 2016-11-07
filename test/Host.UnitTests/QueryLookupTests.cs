namespace Host.UnitTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
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
        public void GetEnumeratorShouldReturnAllTheGroups()
        {
            var lookup = new QueryLookup("?key1=value1&key2=value2");

            IGrouping<string, string>[] groups = lookup.ToArray();

            Assert.That(groups, Has.Length.EqualTo(2));
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
        public void NonGenericGetEnumeratorShouldReturnAllTheGroups()
        {
            IEnumerable lookup = new QueryLookup("?key1=value1&key2=value2");

            IEnumerator enumerator = lookup.GetEnumerator();
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.MoveNext(), Is.False);
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

        [Test]
        public void GroupingsShouldHaveTheCorrectKey()
        {
            var lookup = new QueryLookup("?key=value");
            IGrouping<string, string> group = lookup.Single();

            Assert.That(group.Key, Is.EqualTo("key"));
        }

        [Test]
        public void GroupingsShouldReturnAllTheValues()
        {
            var lookup = new QueryLookup("?key=1&key=2");
            IGrouping<string, string> group = lookup.Single();

            string[] values = group.ToArray();

            Assert.That(values, Is.EqualTo(new[] { "1", "2" }));
        }

        [Test]
        public void GroupingsNonGenericGetEnumeratorShouldReturnTheValues()
        {
            var lookup = new QueryLookup("?key=1&key=2");
            IEnumerable group = lookup.Single();
            IEnumerator enumerator = group.GetEnumerator();

            // We can test the order of the results as well here...
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo("1"));
            Assert.That(enumerator.MoveNext(), Is.True);
            Assert.That(enumerator.Current, Is.EqualTo("2"));
            Assert.That(enumerator.MoveNext(), Is.False);
        }

        [Test]
        public void GroupingsShouldBeAReadOnlyCollection()
        {
            var lookup = new QueryLookup("?key=value");
            ICollection<string> group = (ICollection<string>)lookup.Single();

            Assert.That(group.Count, Is.EqualTo(1));
            Assert.That(group.IsReadOnly, Is.True);
            Assert.That(() => group.Add(""), Throws.InstanceOf<NotSupportedException>());
            Assert.That(() => group.Clear(), Throws.InstanceOf<NotSupportedException>());
            Assert.That(() => group.Remove(""), Throws.InstanceOf<NotSupportedException>());
        }

        [Test]
        public void GroupingsContainsShouldReturnWhetherTheValueIsPresentOrNot()
        {
            var lookup = new QueryLookup("?key=value");
            ICollection<string> group = (ICollection<string>)lookup.Single();

            Assert.That(group.Contains("value"), Is.True);
            Assert.That(group.Contains("unknown"), Is.False);
        }

        [Test]
        public void GroupingsCopyToShouldCopyAllTheValues()
        {
            var lookup = new QueryLookup("?key=1&key=2");
            ICollection<string> group = (ICollection<string>)lookup.Single();

            string[] target = new string[3];
            group.CopyTo(target, 1);

            Assert.That(target, Is.EqualTo(new[] { null, "1", "2" }));
        }
    }
}
