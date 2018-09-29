namespace DataAccess.UnitTests.Parsing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Crest.DataAccess.Parsing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class QueryParserTests
    {
        private readonly Dictionary<string, object> parameters;
        private readonly QueryParser parser;
        private readonly ILookup<string, string> query;

        private QueryParserTests()
        {
            this.parameters = new Dictionary<string, object>();
            this.query = Substitute.For<ILookup<string, string>>();
            this.parser = new QueryParser(this.parameters, this.query);
        }

        private void SetQuery(string key, params string[] values)
        {
            ILookup<string, string> lookup = values.ToLookup(_ => key);
            this.query.GetEnumerator().Returns(lookup.GetEnumerator());
            this.query[null].ReturnsForAnyArgs(ci => lookup[ci.Arg<string>()]);
        }

        public sealed class GetFilters : QueryParserTests
        {
            [Fact]
            public void ShouldDefaultToEqualsIfNoMethodSpecified()
            {
                this.SetQuery(nameof(ExampleClass.Property), "value");

                FilterInfo result = this.parser.GetFilters(typeof(ExampleClass)).Single();

                result.Method.Should().Be(FilterMethod.Equals);
                result.Property.Name.Should().Be(nameof(ExampleClass.Property));
                result.Value.Should().Be("value");
            }

            [Fact]
            public void ShouldIgnorePropertiesThatAreCaptured()
            {
                this.SetQuery(nameof(ExampleClass.Property), "value");
                this.parameters.Add(nameof(ExampleClass.Property), null);

                IEnumerable<FilterInfo> result = this.parser.GetFilters(typeof(ExampleClass));

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldIgnoreUnknownMethods()
            {
                this.SetQuery(nameof(ExampleClass.Property), "method:value");

                IEnumerable<FilterInfo> result = this.parser.GetFilters(typeof(ExampleClass));

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldIgnoreUnknownProperties()
            {
                this.SetQuery("unknown", "value");

                IEnumerable<FilterInfo> result = this.parser.GetFilters(typeof(ExampleClass));

                result.Should().BeEmpty();
            }

            [Theory]
            [InlineData("eq", FilterMethod.Equals)]
            [InlineData("ne", FilterMethod.NotEquals)]
            [InlineData("in", FilterMethod.In)]
            [InlineData("gt", FilterMethod.GreaterThan)]
            [InlineData("ge", FilterMethod.GreaterThanOrEqual)]
            [InlineData("lt", FilterMethod.LessThan)]
            [InlineData("le", FilterMethod.LessThanOrEqual)]
            [InlineData("contains", FilterMethod.Contains)]
            [InlineData("endswith", FilterMethod.EndsWith)]
            [InlineData("startswith", FilterMethod.StartsWith)]
            internal void ShouldParseTheFilterMethod(string method, FilterMethod expected)
            {
                this.SetQuery(nameof(ExampleClass.Property), method + ":value");

                FilterInfo info = this.parser.GetFilters(typeof(ExampleClass)).Single();

                info.Method.Should().Be(expected);
                info.Value.Should().Be("value");
            }
        }

        public sealed class GetSorting : QueryParserTests
        {
            private const string SortParameter = "order";

            [Fact]
            public void ShouldDefaultToAscending()
            {
                this.SetQuery(SortParameter, nameof(ExampleClass.Property));

                (PropertyInfo property, SortDirection direction) =
                    this.parser.GetSorting(typeof(ExampleClass)).Single();

                direction.Should().Be(SortDirection.Ascending);
                property.Name.Should().Be(nameof(ExampleClass.Property));
            }

            [Fact]
            public void ShouldIgnoreUnknownMethods()
            {
                this.SetQuery(SortParameter, "method:" + nameof(ExampleClass.Property));

                IEnumerable<(PropertyInfo property, SortDirection direction)> result =
                    this.parser.GetSorting(typeof(ExampleClass));

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldIgnoreUnknownProperties()
            {
                this.SetQuery(SortParameter, "unknown");

                IEnumerable<(PropertyInfo property, SortDirection direction)> result =
                    this.parser.GetSorting(typeof(ExampleClass));

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnMultipleValuesForMultipleQueryValues()
            {
                this.SetQuery(SortParameter, nameof(ExampleClass.Property), nameof(ExampleClass.Other));

                (PropertyInfo property, SortDirection direction)[] result =
                    this.parser.GetSorting(typeof(ExampleClass)).ToArray();

                result.Should().HaveCount(2);
                result.Select(x => x.property.Name).Should().BeEquivalentTo(
                    nameof(ExampleClass.Property), nameof(ExampleClass.Other));
            }

            [Fact]
            public void ShouldReturnMultipleValuesSeparatedByCommas()
            {
                this.SetQuery(SortParameter, nameof(ExampleClass.Property) + "," + nameof(ExampleClass.Other));

                (PropertyInfo property, SortDirection direction)[] result =
                    this.parser.GetSorting(typeof(ExampleClass)).ToArray();

                result.Should().HaveCount(2);
                result.Select(x => x.property.Name).Should().BeEquivalentTo(
                    nameof(ExampleClass.Property), nameof(ExampleClass.Other));
            }

            [Theory]
            [InlineData("asc", SortDirection.Ascending)]
            [InlineData("desc", SortDirection.Descending)]
            internal void ShouldParseTheSortDirection(string method, SortDirection expected)
            {
                this.SetQuery(SortParameter, method + ":" + nameof(ExampleClass.Property));

                (PropertyInfo property, SortDirection direction) =
                    this.parser.GetSorting(typeof(ExampleClass)).Single();

                direction.Should().Be(expected);
            }
        }

        private sealed class ExampleClass
        {
            public string Other { get; set; }
            public string Property { get; set; }
        }
    }
}
