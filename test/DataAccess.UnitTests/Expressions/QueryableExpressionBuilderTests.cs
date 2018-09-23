namespace DataAccess.UnitTests.Expressions
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Crest.DataAccess.Expressions;
    using Crest.DataAccess.Parsing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class QueryableExpressionBuilderTests
    {
        private static readonly PropertyInfo SimpleClassInteger =
            typeof(SimpleClass).GetProperty(nameof(SimpleClass.Integer));

        private static readonly PropertyInfo SimpleClassString =
            typeof(SimpleClass).GetProperty(nameof(SimpleClass.String));

        private readonly QueryableExpressionBuilder builder;
        private readonly MappingCache mappingCache;

        public QueryableExpressionBuilderTests()
        {
            this.mappingCache = Substitute.For<MappingCache>();
            this.builder = new QueryableExpressionBuilder(
                this.mappingCache);
        }

        public sealed class CreateFilter : QueryableExpressionBuilderTests
        {
            [Fact]
            public void ShouldFilterForValuesInASet()
            {
                SimpleClass[] source =
                {
                    new SimpleClass { String = "one" },
                    new SimpleClass { String = "two" },
                    new SimpleClass { String = "three" },
                };

                var result = (IQueryable<SimpleClass>)this.builder.CreateFilter(
                    source.AsQueryable(),
                    SimpleClassString,
                    FilterMethod.In,
                    "one,three,five");

                result.Select(s => s.String)
                      .Should().BeEquivalentTo("one", "three");
            }

            [Fact]
            public void ShouldMapTheProperties()
            {
                Expression<Func<string, bool>> noMatch = x => false;
                this.mappingCache.CreateMemberAccess(null, null, null)
                    .ReturnsForAnyArgs(noMatch);

                this.builder.CreateFilter(
                    Enumerable.Empty<string>().AsQueryable(),
                    SimpleClassString,
                    FilterMethod.Equals,
                    "abc");

                this.mappingCache.Received().CreateMemberAccess(
                    typeof(string),
                    SimpleClassString,
                    Arg.Any<Func<Expression, Expression>>());
            }

            [Fact]
            public void ShouldThrowIfUnableToConvertTheValue()
            {
                Action action = () => this.builder.CreateFilter(
                    Enumerable.Empty<SimpleClass>().AsQueryable(),
                    SimpleClassInteger,
                    FilterMethod.Equals,
                    "abc");

                action.Should().Throw<InvalidOperationException>();
            }

            [Theory]
            [InlineData(FilterMethod.GreaterThan, "2", "1,2,3", "3")]
            [InlineData(FilterMethod.GreaterThanOrEqual, "2", "1,2,3", "2,3")]
            [InlineData(FilterMethod.LessThan, "2", "1,2,3", "1")]
            [InlineData(FilterMethod.LessThanOrEqual, "2", "1,2,3", "1,2")]
            internal void ShouldFilterIntegerResults(
                FilterMethod method,
                string filter,
                string values,
                string expected)
            {
                IQueryable<SimpleClass> query =
                    values.Split(',')
                    .Select(x => new SimpleClass { Integer = int.Parse(x, NumberFormatInfo.InvariantInfo) })
                    .AsQueryable();

                var result = (IQueryable<SimpleClass>)this.builder.CreateFilter(
                    query,
                    SimpleClassInteger,
                    method,
                    filter);

                result.Select(s => s.Integer.ToString(NumberFormatInfo.InvariantInfo))
                      .Should().BeEquivalentTo(expected.Split(','));
            }

            [Theory]
            [InlineData(FilterMethod.Contains, "e", "abc,def", "def")]
            [InlineData(FilterMethod.EndsWith, "c", "abc,def", "abc")]
            [InlineData(FilterMethod.Equals, "cd", "ab,cd", "cd")]
            [InlineData(FilterMethod.NotEquals, "cd", "ab,cd", "ab")]
            [InlineData(FilterMethod.StartsWith, "d", "abc,def", "def")]
            internal void ShouldFilterStringResults(
                FilterMethod method,
                string filter,
                string values,
                string expected)
            {
                IQueryable<SimpleClass> query =
                    values.Split(',')
                    .Select(x => new SimpleClass { String = x })
                    .AsQueryable();

                var result = (IQueryable<SimpleClass>)this.builder.CreateFilter(
                    query,
                    SimpleClassString,
                    method,
                    filter);

                result.Select(s => s.String)
                      .Should().BeEquivalentTo(expected.Split(','));
            }
        }

        public sealed class CreateSort : QueryableExpressionBuilderTests
        {
            [Fact]
            public void ShouldAllowMultipleSorting()
            {
                SimpleClass[] values =
                {
                    new SimpleClass { String = "a", Integer = 1 },
                    new SimpleClass { String = "b", Integer = 1 },
                    new SimpleClass { String = "a", Integer = 2 },
                };

                IQueryable<SimpleClass> result = this.CreateMultiSort(values, SortDirection.Ascending);

                result.Select(s => s.String)
                      .Should().BeEquivalentTo("a", "a", "b");

                result.Select(s => s.Integer)
                      .Should().BeEquivalentTo(1, 2, 1);
            }

            [Fact]
            public void ShouldAllowMultipleSortingDescending()
            {
                SimpleClass[] values =
                {
                    new SimpleClass { String = "a", Integer = 2 },
                    new SimpleClass { String = "b", Integer = 1 },
                    new SimpleClass { String = "b", Integer = 2 },
                };

                IQueryable<SimpleClass> result = this.CreateMultiSort(values, SortDirection.Ascending);

                result.Select(s => s.String)
                      .Should().BeEquivalentTo("b", "b", "a");

                result.Select(s => s.Integer)
                      .Should().BeEquivalentTo(2, 1, 2);
            }

            [Theory]
            [InlineData(SortDirection.Ascending, "a,c,b", "a,b,c")]
            [InlineData(SortDirection.Descending, "a,c,b", "c,b,a")]
            internal void ShouldSortTheResults(SortDirection direction, string input, string expected)
            {
                IQueryable<SimpleClass> query =
                    input.Split(',')
                    .Select(x => new SimpleClass { String = x })
                    .AsQueryable();

                var result = (IQueryable<SimpleClass>)this.builder.CreateSort(
                    query,
                    SimpleClassString,
                    direction);

                result.Select(s => s.String)
                      .Should().BeEquivalentTo(expected.Split(','));
            }

            private IQueryable<SimpleClass> CreateMultiSort(SimpleClass[] values, SortDirection direction)
            {
                IQueryable query = this.builder.CreateSort(
                    values.AsQueryable(),
                    SimpleClassString,
                    direction);

                return (IQueryable<SimpleClass>)this.builder.CreateSort(
                    query,
                    SimpleClassInteger,
                    direction);
            }
        }

        private sealed class SimpleClass
        {
            public int Integer { get; set; }

            public string String { get; set; }
        }
    }
}
