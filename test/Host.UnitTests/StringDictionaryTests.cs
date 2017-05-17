namespace Host.UnitTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Crest.Host;
    using FluentAssertions;
    using Xunit;

    public class StringDictionaryTests
    {
        private readonly StringDictionary<int> dictionary = new StringDictionary<int>();

        public sealed class Add : StringDictionaryTests
        {
            [Fact]
            public void ShouldAddTheKeyValuePair()
            {
                ((ICollection<KeyValuePair<string, int>>)this.dictionary).Add(
                    new KeyValuePair<string, int>("key", 123));

                this.dictionary["key"].Should().Be(123);
            }

            [Fact]
            public void ShouldResizeToFitAllTheValues()
            {
                for (int i = 0; i < 6; i++)
                {
                    this.dictionary.Add(i.ToString(), i);
                }

                this.dictionary.Count.Should().Be(6);
            }
        }

        public sealed class Clear : StringDictionaryTests
        {
            [Fact]
            public void ShouldRemoveAllKeys()
            {
                this.dictionary.Add("key", 123);

                this.dictionary.Clear();

                this.dictionary["key"].Should().Be(0);
            }

            [Fact]
            public void ShouldResetTheCount()
            {
                this.dictionary.Add("key", 123);

                this.dictionary.Clear();

                this.dictionary.Count.Should().Be(0);
            }
        }

        public sealed class Contains : StringDictionaryTests
        {
            [Fact]
            public void ShouldThrowNotSupportedException()
            {
                this.dictionary.Invoking<IDictionary<string, int>>(d => d.Contains(default(KeyValuePair<string, int>)))
                    .ShouldThrow<NotSupportedException>();
            }
        }

        public sealed class ContainsKey : StringDictionaryTests
        {
            [Fact]
            public void ShouldReturnFalseIfTheKeyDoesNotExist()
            {
                bool result = this.dictionary.ContainsKey("unknown");

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfTheKeyExists()
            {
                this.dictionary.Add("key", 123);

                bool result = this.dictionary.ContainsKey("key");

                result.Should().BeTrue();
            }
        }

        public sealed class CopyTo : StringDictionaryTests
        {
            [Fact]
            public void ShouldThrowNotSupportedException()
            {
                this.dictionary.Invoking<IDictionary<string, int>>(d => d.CopyTo(null, 0))
                    .ShouldThrow<NotSupportedException>();
            }
        }

        public sealed class Count : StringDictionaryTests
        {
            [Fact]
            public void ShouldReturnTheNumberOfAddedValues()
            {
                this.dictionary.Add("one", 1);
                this.dictionary.Add("two", 2);

                this.dictionary.Count.Should().Be(2);
            }
        }

        public sealed class GetEnumerator : StringDictionaryTests
        {
            [Fact]
            public void NonGenericEnumeratorShouldReturnTheValues()
            {
                this.dictionary.Add("one", 1);

                IEnumerator enumerator = ((IEnumerable)this.dictionary).GetEnumerator();

                enumerator.MoveNext().Should().BeTrue();
                enumerator.MoveNext().Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnAllTheValues()
            {
                this.dictionary.Add("one", 1);
                this.dictionary.Add("two", 2);

                using (IEnumerator<KeyValuePair<string, int>> enumerator = this.dictionary.GetEnumerator())
                {
                    enumerator.MoveNext().Should().BeTrue();
                    enumerator.Current.Should().Be(new KeyValuePair<string, int>("one", 1));

                    enumerator.MoveNext().Should().BeTrue();
                    enumerator.Current.Should().Be(new KeyValuePair<string, int>("two", 2));

                    enumerator.MoveNext().Should().BeFalse();
                }
            }
        }

        public sealed class Index : StringDictionaryTests
        {
            [Fact]
            public void ShouldIgnoreTheCaseOfTheKey()
            {
                this.dictionary.Add("one", 1);

                this.dictionary["ONE"].Should().Be(1);
            }

            [Fact]
            public void ShouldReturnTheDefaultValueIfTheKeyDoesNotExist()
            {
                this.dictionary["unknown"].Should().Be(0);
            }

            [Fact]
            public void ShouldReturnTheValueAssociatedWithTheKey()
            {
                this.dictionary.Add("one", 1);
                this.dictionary.Add("two", 2);

                this.dictionary["two"].Should().Be(2);
            }

            [Fact]
            public void ShouldThrowNotSupportedForSettingAValue()
            {
                this.dictionary.Invoking(d => d["key"] = 123)
                    .ShouldThrow<NotSupportedException>();
            }
        }

        public sealed class IsReadOnly : StringDictionaryTests
        {
            [Fact]
            public void ShouldReturnFalse()
            {
                this.dictionary.IsReadOnly.Should().BeFalse();
            }
        }

        public sealed class Keys : StringDictionaryTests
        {
            [Fact]
            public void ShouldReturnAllTheAddedKeys()
            {
                this.dictionary.Add("one", 1);
                this.dictionary.Add("two", 2);

                this.dictionary.Keys.Should().BeEquivalentTo("one", "two");
            }

            [Fact]
            public void ShouldReturnAnEmptyCollectionForAnEmptyDictionary()
            {
                this.dictionary.Keys.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnAnIEnumarableValue()
            {
                IEnumerable<string> keys =
                    ((IReadOnlyDictionary<string, int>)this.dictionary).Keys;

                keys.Should().NotBeNull();
            }
        }

        public sealed class Remove : StringDictionaryTests
        {
            [Fact]
            public void ShouldThrowNotSupportedException()
            {
                this.dictionary.Invoking<IDictionary<string, int>>(d => d.Remove("key"))
                    .ShouldThrow<NotSupportedException>();
            }

            [Fact]
            public void ShouldThrowNotSupportedExceptionForKeyValuePairs()
            {
                this.dictionary.Invoking<IDictionary<string, int>>(d => d.Remove(default(KeyValuePair<string, int>)))
                    .ShouldThrow<NotSupportedException>();
            }
        }

        public sealed class TryGetValue : StringDictionaryTests
        {
            [Fact]
            public void ShouldReturnFalseIfTheKeyDoesNotExist()
            {
                bool result = this.dictionary.TryGetValue("unknown", out int value);

                result.Should().BeFalse();
                value.Should().Be(0);
            }

            [Fact]
            public void ShouldReturnTrueIfTheKeyExists()
            {
                this.dictionary.Add("key", 123);

                bool result = this.dictionary.TryGetValue("key", out int value);

                result.Should().BeTrue();
                value.Should().Be(123);
            }
        }

        public sealed class Values : StringDictionaryTests
        {
            [Fact]
            public void ShouldReturnAllTheAddedKeys()
            {
                this.dictionary.Add("one", 1);
                this.dictionary.Add("two", 2);

                this.dictionary.Values.Should().BeEquivalentTo(1, 2);
            }

            [Fact]
            public void ShouldReturnAnEmptyCollectionForAnEmptyDictionary()
            {
                this.dictionary.Values.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnAnIEnumarableValue()
            {
                IEnumerable<int> values =
                    ((IReadOnlyDictionary<string, int>)this.dictionary).Values;

                values.Should().NotBeNull();
            }
        }
    }
}
