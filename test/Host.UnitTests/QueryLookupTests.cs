namespace Host.UnitTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Host;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class QueryLookupTests
    {
        [Test]
        public void ShouldHandleEmptyQueryStrings()
        {
            var lookup = new QueryLookup(string.Empty);

            lookup.Count.Should().Be(0);
        }

        [Test]
        public void ShouldHandleKeysWithoutValues()
        {
            var lookup = new QueryLookup("?key");

            lookup.Contains("key").Should().BeTrue();
        }

        [Test]
        public void ShouldIgnoreTheCaseOfPercentageEscapedValues()
        {
            var lookup = new QueryLookup("?%2A=%2a");

            lookup["*"].Single().Should().Be("*");
        }

        [Test]
        public void ShouldUnescapePrecentageEscapedKeys()
        {
            var lookup = new QueryLookup("?%41");

            lookup.Contains("A").Should().BeTrue();
        }

        [Test]
        public void ShouldUnescapeSpacesEncodedAsPlus()
        {
            var lookup = new QueryLookup("?a+b=c+d");

            lookup["a b"].Single().Should().Be("c d");
        }

        [TestFixture]
        public sealed class Constructor : QueryLookupTests
        {
            [TestCase("%1")]
            [TestCase("%0G")]
            public void ShouldThrowUriFormatExceptionForInvalidPercentageEscapedValues(string value)
            {
                Action action = () => new QueryLookup("?" + value);

                action.ShouldThrow<UriFormatException>();
            }
        }

        [TestFixture]
        public sealed class Contains : QueryLookupTests
        {
            [Test]
            public void ShouldReturnFalseIfTheKeyIsNotPresent()
            {
                var lookup = new QueryLookup("?key=value");

                lookup.Contains("unknown").Should().BeFalse();
            }

            [Test]
            public void ShouldReturnTrueIfTheKeyIsPresent()
            {
                var lookup = new QueryLookup("key=value");

                lookup.Contains("key").Should().BeTrue();
            }
        }

        [TestFixture]
        public sealed class Count : QueryLookupTests
        {
            [Test]
            public void ShouldReturnTheNumberOfUniqueKeys()
            {
                var lookup = new QueryLookup("?key1=_&key2=_&key1=_");

                lookup.Count.Should().Be(2);
            }
        }

        [TestFixture]
        public sealed class GetEnumerator : QueryLookupTests
        {
            [Test]
            public void ShouldReturnAllTheGroups()
            {
                var lookup = new QueryLookup("?key1=value1&key2=value2");

                IGrouping<string, string>[] groups = lookup.ToArray();

                groups.Should().HaveCount(2);
            }
        }

        [TestFixture]
        public sealed class Groupings : QueryLookupTests
        {
            [Test]
            public void ContainsShouldReturnWhetherTheValueIsPresentOrNot()
            {
                var lookup = new QueryLookup("?key=value");
                ICollection<string> group = (ICollection<string>)lookup.Single();

                group.Contains("value").Should().BeTrue();
                group.Contains("unknown").Should().BeFalse();
            }

            [Test]
            public void CopyToShouldCopyAllTheValues()
            {
                var lookup = new QueryLookup("?key=1&key=2");
                ICollection<string> group = (ICollection<string>)lookup.Single();

                string[] target = new string[3];
                group.CopyTo(target, 1);

                target.Should().BeEquivalentTo(null, "1", "2");
            }

            [Test]
            public void NonGenericGetEnumeratorShouldReturnTheValues()
            {
                var lookup = new QueryLookup("?key=1&key=2");
                IEnumerable group = lookup.Single();
                IEnumerator enumerator = group.GetEnumerator();

                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Should().Be("1");
                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Should().Be("2");
                enumerator.MoveNext().Should().BeFalse();
            }

            [Test]
            public void ShouldBeAReadOnlyCollection()
            {
                var lookup = new QueryLookup("?key=value");
                ICollection<string> group = (ICollection<string>)lookup.Single();

                group.Count.Should().Be(1);
                group.IsReadOnly.Should().BeTrue();
                group.Invoking(g => g.Add("")).ShouldThrow<NotSupportedException>();
                group.Invoking(g => g.Clear()).ShouldThrow<NotSupportedException>();
                group.Invoking(g => g.Remove("")).ShouldThrow<NotSupportedException>();
            }

            [Test]
            public void ShouldHaveTheCorrectKey()
            {
                var lookup = new QueryLookup("?key=value");
                IGrouping<string, string> group = lookup.Single();

                group.Key.Should().Be("key");
            }

            [Test]
            public void ShouldReturnAllTheValues()
            {
                var lookup = new QueryLookup("?key=1&key=2");
                IGrouping<string, string> group = lookup.Single();

                string[] values = group.ToArray();

                values.Should().BeEquivalentTo("1", "2");
            }
        }

        [TestFixture]
        public sealed class Index : QueryLookupTests
        {
            [Test]
            public void ShouldReturnAllTheValues()
            {
                var lookup = new QueryLookup("?key=1&key=2");

                lookup["key"].ToList().Should().BeEquivalentTo("1", "2");
            }

            [Test]
            public void ShouldReturnAnEmptySequenceIfTheKeyDoesNotExist()
            {
                var lookup = new QueryLookup("?key=value");

                lookup["unknown"].Should().BeEmpty();
            }
        }

        [TestFixture]
        public sealed class NonGenericGetEnumerator : QueryLookupTests
        {
            [Test]
            public void ShouldReturnAllTheGroups()
            {
                IEnumerable lookup = new QueryLookup("?key1=value1&key2=value2");

                IEnumerator enumerator = lookup.GetEnumerator();
                enumerator.MoveNext().Should().BeTrue();
                enumerator.MoveNext().Should().BeTrue();
                enumerator.MoveNext().Should().BeFalse();
            }
        }
    }
}
