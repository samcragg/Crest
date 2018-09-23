namespace DataAccess.UnitTests.Expressions
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using Crest.DataAccess;
    using Crest.DataAccess.Expressions;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class MappingCacheTests
    {
        private static readonly PropertyInfo ServiceObjectProperty =
            typeof(ServiceObject).GetProperty(nameof(ServiceObject.Property));

        private readonly Lazy<MappingCache> cache;
        private readonly IMappingProvider provider;

        private MappingCacheTests()
        {
            this.provider = Substitute.For<IMappingProvider>();
            this.provider.From.Returns(typeof(ServiceObject));
            this.provider.To.Returns(typeof(DataAccessObject));

            this.cache = new Lazy<MappingCache>(
                () => new MappingCache(new[] { this.provider }));
        }

        private MappingCache Cache => this.cache.Value;

        public sealed class TryCreateMemberAccess : MappingCacheTests
        {
            [Fact]
            public void ShouldReturnAnExpressionForAccessingTheMappedProperty()
            {
                this.provider.GenerateMappings().Returns(CreateMapping());

                LambdaExpression result = this.Cache.CreateMemberAccess(
                    typeof(DataAccessObject),
                    ServiceObjectProperty,
                    x => x);

                result.Body.Should().BeAssignableTo<MemberExpression>()
                      .Which.Member.Name.Should().Be(nameof(DataAccessObject.DbField));
            }

            [Fact]
            public void ShouldThrowForUnknownProperties()
            {
                Action action = () => this.Cache.CreateMemberAccess(
                    typeof(DataAccessObject),
                    ServiceObjectProperty,
                    x => x);

                action.Should().Throw<InvalidOperationException>();
            }

            [Fact]
            public void ShouldThrowForUnknownSourceTypes()
            {
                Action action = () => this.Cache.CreateMemberAccess(
                    typeof(UnknownClass),
                    ServiceObjectProperty,
                    x => x);

                action.Should().Throw<InvalidOperationException>();
            }

            [Fact]
            public void ShouldTransformTheExpression()
            {
                this.provider.GenerateMappings().Returns(CreateMapping());

                LambdaExpression result = this.Cache.CreateMemberAccess(
                    typeof(DataAccessObject),
                    ServiceObjectProperty,
                    _ => Expression.Constant("123"));

                result.Body.Should().BeAssignableTo<ConstantExpression>();
            }

            private static Expression CreateMapping()
            {
                // Create an expression for: d.DbField = s.Property
                Expression<Func<DataAccessObject, string>> data = d => d.DbField;
                Expression<Func<ServiceObject, string>> service = s => s.Property;
                return Expression.Assign(data.Body, service.Body);
            }
        }

        private sealed class DataAccessObject
        {
            public string DbField { get; set; }
        }

        private sealed class ServiceObject
        {
            public string Property { get; set; }
        }

        private sealed class UnknownClass
        {
        }
    }
}
