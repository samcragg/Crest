namespace Host.UnitTests.Conversion
{
    using System;
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
    }
}
