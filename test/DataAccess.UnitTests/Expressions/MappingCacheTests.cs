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
        private readonly IMappingInfoFactory factory;

        private MappingCacheTests()
        {
            this.factory = Substitute.For<IMappingInfoFactory>();

            this.cache = new Lazy<MappingCache>(
                () => new MappingCache(new[] { this.factory }));
        }

        private MappingCache Cache => this.cache.Value;

        public sealed class TryCreateMemberAccess : MappingCacheTests
        {
            [Fact]
            public void ShouldReturnAnExpressionForAccessingTheMappedProperty()
            {
                this.SetMappingInfo(CreateMapping());

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
                this.SetMappingInfo(CreateMapping());

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

            private void SetMappingInfo(Expression expression)
            {
                var mappingInfo = new MappingInfo(
                    typeof(DataAccessObject),
                    typeof(ServiceObject),
                    expression);
                this.factory.GetMappingInformation().Returns(new[] { mappingInfo });
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
