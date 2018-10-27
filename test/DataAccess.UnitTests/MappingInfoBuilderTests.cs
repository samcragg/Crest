namespace DataAccess.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Crest.DataAccess;
    using FluentAssertions;
    using Xunit;

    public class MappingInfoBuilderTests
    {
        private readonly MappingInfoBuilder<FakeSourceType, FakeDestinationType> builder =
            new MappingInfoBuilder<FakeSourceType, FakeDestinationType>();

        public sealed class ToMappingInfo : MappingInfoBuilderTests
        {
            [Fact]
            public void ShouldReturnAnExpressionThatAssignsTheProperties()
            {
                var destination = new FakeDestinationType();
                var source = new FakeSourceType { SourceProperty = "value" };
                this.builder.Map(s => s.SourceProperty).To(d => d.DestinationProperty);

                var mapping = this.builder.ToMappingInfo();
                Action<FakeDestinationType, FakeSourceType> lambda = CreateAction(mapping.Mapping);

                lambda(destination, source);

                destination.DestinationProperty.Should().Be("value");
            }

            [Fact]
            public void ShouldReturnTheDestinationType()
            {
                var mapping = this.builder.ToMappingInfo();

                mapping.Destination.Should().Be<FakeDestinationType>();
            }

            [Fact]
            public void ShouldReturnTheSourceType()
            {
                var mapping = this.builder.ToMappingInfo();

                mapping.Source.Should().Be<FakeSourceType>();
            }

            private static Action<FakeDestinationType, FakeSourceType> CreateAction(Expression mappings)
            {
                var parameterVisitor = new ParameterVisitor();
                parameterVisitor.Visit(mappings);
                Action<FakeDestinationType, FakeSourceType> lambda =
                    Expression.Lambda<Action<FakeDestinationType, FakeSourceType>>(
                        mappings,
                        parameterVisitor.Parameters).Compile();
                return lambda;
            }

            private sealed class ParameterVisitor : ExpressionVisitor
            {
                private readonly List<ParameterExpression> parameters = new List<ParameterExpression>();

                internal ParameterExpression[] Parameters => this.parameters.ToArray();

                protected override Expression VisitParameter(ParameterExpression node)
                {
                    this.parameters.Add(node);
                    return base.VisitParameter(node);
                }
            }
        }

        private class FakeDestinationType
        {
            public string DestinationProperty { get; set; }
        }

        private class FakeSourceType
        {
            public string SourceProperty { get; set; }
        }
    }
}
