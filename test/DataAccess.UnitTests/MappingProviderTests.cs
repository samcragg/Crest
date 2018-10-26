namespace DataAccess.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Crest.DataAccess;
    using FluentAssertions;
    using Xunit;

    public class MappingProviderTests
    {
        private readonly MappingProvider<FakeSourceType, FakeDestinationType> provider =
            new SimpleMappingProvider();

        public sealed class Destination : MappingProviderTests
        {
            [Fact]
            public void ShouldReturnTheTDestValue()
            {
                Type result = this.provider.Destination;

                result.Should().Be<FakeDestinationType>();
            }
        }

        public sealed class GenerateMappings : MappingProviderTests
        {
            [Fact]
            public void ShouldReturnAnExpressionThatAssignsTheProperties()
            {
                var destination = new FakeDestinationType();
                var source = new FakeSourceType { SourceProperty = "value" };

                Expression mappings = this.provider.GenerateMappings();
                Action<FakeDestinationType, FakeSourceType> lambda = CreateAction(mappings);

                lambda(destination, source);

                destination.DestinationProperty.Should().Be("value");
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

        public sealed class Source : MappingProviderTests
        {
            [Fact]
            public void ShouldReturnTheTSourceValue()
            {
                Type result = this.provider.Source;

                result.Should().Be<FakeSourceType>();
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

        private class SimpleMappingProvider : MappingProvider<FakeSourceType, FakeDestinationType>
        {
            public SimpleMappingProvider()
            {
                this.Map(s => s.SourceProperty).To(d => d.DestinationProperty);
            }
        }
    }
}
