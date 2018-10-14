namespace DataAccess.UnitTests
{
    using System;
    using System.Dynamic;
    using System.Linq;
    using System.Linq.Expressions;
    using Crest.DataAccess;
    using NSubstitute;
    using Xunit;

    public class QueryOptionsTests
    {
        private readonly dynamic expando;
        private readonly QueryOptions<_SimpleData> options;
        private readonly IQueryable<_SimpleData> query;

        private QueryOptionsTests()
        {
            this.expando = new ExpandoObject();
            this.query = Substitute.For<IQueryable<_SimpleData>>();
            this.query.ElementType.Returns(typeof(_SimpleData));
            this.query.Expression.Returns(Array.Empty<_SimpleData>().AsQueryable().Expression);
            this.query.Provider.CreateQuery(null).ReturnsForAnyArgs(this.query);
            this.options = QueryableExtensions.Apply(this.query);
        }

        // Public so it can be created by NSubstitute
        public class _SimpleData
        {
            public string Property { get; set; }
        }

        public sealed class Filter : QueryOptionsTests
        {
            [Fact]
            public void ShouldApplyWhereToTheQuery()
            {
                this.expando.property = new[] { "value" };

                this.options.Filter(this.expando);

                this.query.Provider.Received().CreateQuery(
                    Arg.Is<Expression>(e => ((MethodCallExpression)e).Method.Name == "Where"));
            }
        }

        public sealed class FilterAndSort : QueryOptionsTests
        {
            [Fact]
            public void ShouldApplyWhereBeforeOrderByToTheQuery()
            {
                this.expando.order = new[] { nameof(_SimpleData.Property) };
                this.expando.property = new[] { "value" };

                this.options.FilterAndSort(this.expando);

                Received.InOrder(() =>
                {
                    this.query.Provider.CreateQuery(
                        Arg.Is<Expression>(e => ((MethodCallExpression)e).Method.Name == "Where"));

                    this.query.Provider.CreateQuery(
                        Arg.Is<Expression>(e => ((MethodCallExpression)e).Method.Name == "OrderBy"));
                });
            }
        }

        public sealed class Sort : QueryOptionsTests
        {
            [Fact]
            public void ShouldApplyOrderByToTheQuery()
            {
                this.expando.order = new[] { nameof(_SimpleData.Property) };

                this.options.Sort(this.expando);

                this.query.Provider.Received().CreateQuery(
                    Arg.Is<Expression>(e => ((MethodCallExpression)e).Method.Name == "OrderBy"));
            }
        }
    }
}
