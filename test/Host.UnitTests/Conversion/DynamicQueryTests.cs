namespace Host.UnitTests.Conversion
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class DynamicQueryTests
    {
        private readonly Lazy<DynamicQuery> dynamicQuery;
        private readonly IReadOnlyDictionary<string, object> parameters;
        private readonly ILookup<string, string> queryKeyValues;

        private DynamicQueryTests()
        {
            this.queryKeyValues = Substitute.For<ILookup<string, string>>();
            this.parameters = Substitute.For<IReadOnlyDictionary<string, object>>();
            this.dynamicQuery = new Lazy<DynamicQuery>(() => new DynamicQuery(
                this.queryKeyValues,
                this.parameters));
        }

        private DynamicQuery DynamicQuery => this.dynamicQuery.Value;

        private void SetParameters(params string[] names)
        {
            this.parameters.ContainsKey(null)
                .ReturnsForAnyArgs(ci => names.Contains(ci.Arg<string>()));
        }

        private void SetQueryValues(params (string key, string value)[] keyValues)
        {
            ILookup<string, string> lookup = keyValues.ToLookup(x => x.key, x => x.value);
            this.queryKeyValues.GetEnumerator().Returns(lookup.GetEnumerator());
        }

        public sealed class ContainsKey : DynamicQueryTests
        {
            [Fact]
            public void ShouldReturnFalseIfTheKeyDoesNotExist()
            {
                this.SetQueryValues(("key", "value"));

                bool result = this.DynamicQuery.ContainsKey("other");

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfTheKeyExists()
            {
                this.SetQueryValues(("key", "value"));

                bool result = this.DynamicQuery.ContainsKey("key");

                result.Should().BeTrue();
            }
        }

        public sealed class Count : DynamicQueryTests
        {
            [Fact]
            public void ShouldReturnTheNumberOfUniqueKeys()
            {
                this.SetQueryValues(("key1", "value1"), ("key2", "value2"), ("key1", "value3"));

                int result = this.DynamicQuery.Count;

                result.Should().Be(2);
            }
        }

        public sealed class GetDynamicMemberNames : DynamicQueryTests
        {
            [Fact]
            public void ShouldNotReturnCapturedParameters()
            {
                this.SetQueryValues(("key", ""), ("captured", ""));
                this.SetParameters("captured");

                IEnumerable<string> result = this.DynamicQuery.GetDynamicMemberNames();

                result.Should().ContainSingle().Which.Should().Be("key");
            }

            [Fact]
            public void ShouldReturnAllTheKeys()
            {
                this.SetQueryValues(("key1", ""), ("key2", ""));

                IEnumerable<string> result = this.DynamicQuery.GetDynamicMemberNames();

                result.Should().BeEquivalentTo("key1", "key2");
            }
        }

        public sealed class GetEnumerator : DynamicQueryTests
        {
            [Fact]
            public void NonGenericEnumeratorShouldReturnAllTheKeyValuePairs()
            {
                this.SetQueryValues(("key", "value"));

                IEnumerator enumerator = ((IEnumerable)this.DynamicQuery).GetEnumerator();

                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Should().BeOfType<KeyValuePair<string, string[]>>()
                    .Which.Key.Should().Be("key");
                enumerator.MoveNext().Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnAllTheKeyValuePairs()
            {
                this.SetQueryValues(("key", "value"));

                IEnumerator<KeyValuePair<string, string[]>> enumerator = this.DynamicQuery.GetEnumerator();

                enumerator.MoveNext().Should().BeTrue();
                enumerator.Current.Key.Should().Be("key");
                enumerator.Current.Value.Should().Equal("value");
                enumerator.MoveNext().Should().BeFalse();
            }
        }

        public sealed class Index : DynamicQueryTests
        {
            [Fact]
            public void ShouldReturnTheValuesForTheKey()
            {
                this.SetQueryValues(("key", "value1"), ("key", "value2"));

                string[] result = this.DynamicQuery["key"];

                result.Should().BeEquivalentTo("value1", "value2");
            }
        }

        public sealed class Keys : DynamicQueryTests
        {
            [Fact]
            public void ShouldReturnTheUniqueKeys()
            {
                this.SetQueryValues(("key1", "value1"), ("key2", "value2"), ("key1", "value3"));

                IEnumerable<string> result = this.DynamicQuery.Keys;

                result.Should().BeEquivalentTo("key1", "key2");
            }
        }

        public sealed class TryGetMember : DynamicQueryTests
        {
            [Fact]
            public void ShouldConvertTheValueToTheDesiredType()
            {
                this.SetQueryValues(("key", "1"));
                this.DynamicQuery.TryGetMember(Member("key"), out object dynamicObject);

                bool converted = ((DynamicObject)dynamicObject).TryConvert(Convert(typeof(int)), out object result);

                converted.Should().BeTrue();
                result.Should().BeOfType<int>().Which.Should().Be(1);
            }

            [Theory]
            [InlineData(typeof(string[]))]
            [InlineData(typeof(IEnumerable<string>))]
            [InlineData(typeof(IReadOnlyCollection<string>))]
            [InlineData(typeof(IReadOnlyList<string>))]
            [InlineData(typeof(ICollection<string>))]
            [InlineData(typeof(IList<string>))]
            public void ShouldReturnAllStringValuesForCollections(Type type)
            {
                this.SetQueryValues(("key", "1"), ("key", "2"));
                this.DynamicQuery.TryGetMember(Member("key"), out object dynamicObject);

                bool converted = ((DynamicObject)dynamicObject).TryConvert(Convert(type), out object result);

                converted.Should().BeTrue();
                result.Should().BeAssignableTo<IEnumerable<string>>()
                      .Which.Should().BeEquivalentTo("1", "2");
            }

            [Fact]
            public void ShouldReturnFalseForUnknownQueryKeys()
            {
                bool result = this.DynamicQuery.TryGetMember(Member("unknownKey"), out object value);

                result.Should().BeFalse();
                value.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheFirstValueForStrings()
            {
                this.SetQueryValues(("key", "1"), ("key", "2"));
                this.DynamicQuery.TryGetMember(Member("key"), out object dynamicObject);

                bool converted = ((DynamicObject)dynamicObject).TryConvert(Convert(typeof(string)), out object result);

                converted.Should().BeTrue();
                result.Should().BeOfType<string>().Which.Should().Be("1");
            }

            [Fact]
            public void ShouldReturnTrueForKnownQueryKeys()
            {
                this.SetQueryValues(("key", "value"));

                bool result = this.DynamicQuery.TryGetMember(Member("key"), out object value);

                result.Should().BeTrue();
                value.Should().NotBeNull();
            }

            private static ConvertBinder Convert(Type type)
            {
                return Substitute.For<ConvertBinder>(type, true);
            }

            private static GetMemberBinder Member(string name)
            {
                return Substitute.For<GetMemberBinder>(name, false);
            }
        }

        public sealed class TryGetValue : DynamicQueryTests
        {
            [Fact]
            public void ShouldReturnTheValuesForTheKey()
            {
                this.SetQueryValues(("key", "value1"), ("key", "value2"));

                bool result = this.DynamicQuery.TryGetValue("key", out string[] values);

                result.Should().BeTrue();
                values.Should().BeEquivalentTo("value1", "value2");
            }
        }

        public sealed class Values : DynamicQueryTests
        {
            [Fact]
            public void ShouldReturnTheGroupedValues()
            {
                this.SetQueryValues(("key", "value1"), ("key", "value2"));

                IEnumerable<string[]> result = this.DynamicQuery.Values;

                result.Should().ContainSingle()
                    .Which.Should().BeEquivalentTo("value1", "value2");
            }
        }
    }
}
