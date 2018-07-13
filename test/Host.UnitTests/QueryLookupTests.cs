﻿namespace Host.UnitTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Host;
    using FluentAssertions;
    using Xunit;

    public class QueryLookupTests
    {
        public sealed class Constructor : QueryLookupTests
        {
            [Fact]
            public void ShouldReturnRentedBuffers()
            {
                lock (FakeArrayPool.LockObject)
                {
                    FakeArrayPool<byte>.Instance.Reset();

                    var lookup = new QueryLookup("?key=value");

                    FakeArrayPool<byte>.Instance.TotalAllocated.Should().Be(0);
                    GC.KeepAlive(lookup);
                }
            }

            [Theory]
            [InlineData("%1")]
            [InlineData("%0G")]
            public void ShouldThrowUriFormatExceptionForInvalidPercentageEscapedValues(string value)
            {
                Action action = () => new QueryLookup("?" + value);

                action.Should().Throw<UriFormatException>();
            }
        }

        public sealed class Contains : QueryLookupTests
        {
            [Fact]
            public void ShouldReturnFalseIfTheKeyIsNotPresent()
            {
                var lookup = new QueryLookup("?key=value");

                lookup.Contains("unknown").Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfTheKeyIsPresent()
            {
                var lookup = new QueryLookup("key=value");

                lookup.Contains("key").Should().BeTrue();
            }
        }

        public sealed class Count : QueryLookupTests
        {
            [Fact]
            public void ShouldReturnTheNumberOfUniqueKeys()
            {
                var lookup = new QueryLookup("?key1=_&key2=_&key1=_");

                lookup.Count.Should().Be(2);
            }
        }

        public sealed class GetEnumerator : QueryLookupTests
        {
            [Fact]
            public void ShouldReturnAllTheGroups()
            {
                var lookup = new QueryLookup("?key1=value1&key2=value2");

                IGrouping<string, string>[] groups = lookup.ToArray();

                groups.Should().HaveCount(2);
            }
        }

        public sealed class Groupings : QueryLookupTests
        {
            [Fact]
            public void ContainsShouldReturnWhetherTheValueIsPresentOrNot()
            {
                var lookup = new QueryLookup("?key=value");
                var group = (ICollection<string>)lookup.Single();

                group.Should().Contain("value");
                group.Should().NotContain("unknown");
            }

            [Fact]
            public void CopyToShouldCopyAllTheValues()
            {
                var lookup = new QueryLookup("?key=1&key=2");
                var group = (ICollection<string>)lookup.Single();

                string[] target = new string[3];
                group.CopyTo(target, 1);

                target.Should().BeEquivalentTo(null, "1", "2");
            }

            [Fact]
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

            [Fact]
            public void ShouldBeAReadOnlyCollection()
            {
                var lookup = new QueryLookup("?key=value");
                var group = (ICollection<string>)lookup.Single();

                group.Count.Should().Be(1);
                group.IsReadOnly.Should().BeTrue();
                group.Invoking(g => g.Add("")).Should().Throw<NotSupportedException>();
                group.Invoking(g => g.Clear()).Should().Throw<NotSupportedException>();
                group.Invoking(g => g.Remove("")).Should().Throw<NotSupportedException>();
            }

            [Fact]
            public void ShouldHaveTheCorrectKey()
            {
                var lookup = new QueryLookup("?key=value");
                IGrouping<string, string> group = lookup.Single();

                group.Key.Should().Be("key");
            }

            [Fact]
            public void ShouldReturnAllTheValues()
            {
                var lookup = new QueryLookup("?key=1&key=2");
                IGrouping<string, string> group = lookup.Single();

                string[] values = group.ToArray();

                values.Should().BeEquivalentTo("1", "2");
            }
        }

        public sealed class Index : QueryLookupTests
        {
            [Fact]
            public void ShouldReturnAllTheValues()
            {
                var lookup = new QueryLookup("?key=1&key=2");

                lookup["key"].ToList().Should().BeEquivalentTo("1", "2");
            }

            [Fact]
            public void ShouldReturnAnEmptySequenceIfTheKeyDoesNotExist()
            {
                var lookup = new QueryLookup("?key=value");

                lookup["unknown"].Should().BeEmpty();
            }
        }

        public sealed class NonGenericGetEnumerator : QueryLookupTests
        {
            [Fact]
            public void ShouldReturnAllTheGroups()
            {
                IEnumerable lookup = new QueryLookup("?key1=value1&key2=value2");

                IEnumerator enumerator = lookup.GetEnumerator();
                enumerator.MoveNext().Should().BeTrue();
                enumerator.MoveNext().Should().BeTrue();
                enumerator.MoveNext().Should().BeFalse();
            }
        }

        public sealed class Parsing : QueryLookupTests
        {
            [Fact]
            public void ShouldHandleEmptyQueryStrings()
            {
                var lookup = new QueryLookup(string.Empty);

                lookup.Should().BeEmpty();
            }

            [Fact]
            public void ShouldHandleKeysWithoutValues()
            {
                var lookup = new QueryLookup("?key");

                lookup.Contains("key").Should().BeTrue();
            }

            [Fact]
            public void ShouldIgnoreTheCaseOfPercentageEscapedValues()
            {
                var lookup = new QueryLookup("?%2A=%2a");

                lookup["*"].Single().Should().Be("*");
            }

            [Fact]
            public void ShouldUnescapePrecentageEscapedKeys()
            {
                var lookup = new QueryLookup("?%41");

                lookup.Contains("A").Should().BeTrue();
            }

            [Fact]
            public void ShouldUnescapeSpacesEncodedAsPlus()
            {
                var lookup = new QueryLookup("?a+b=c+d");

                lookup["a b"].Single().Should().Be("c d");
            }
        }
    }
}
