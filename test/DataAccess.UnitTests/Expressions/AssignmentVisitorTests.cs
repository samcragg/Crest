namespace DataAccess.UnitTests.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using AutoMapper;
    using Crest.DataAccess.Expressions;
    using FluentAssertions;
    using Xunit;

    public class AssignmentVisitorTests
    {
        private readonly AssignmentVisitor visitor =
            new AssignmentVisitor(typeof(DestinationType), typeof(SourceType));

        public sealed class GetAssignments : AssignmentVisitorTests
        {
            [Fact]
            public void ShouldFindAutomappedProperties()
            {
                var config = new MapperConfiguration(c =>
                {
                    c.CreateMap<SourceType, DestinationType>()
                     .ForMember(d => d.Destination, o => o.MapFrom(s => s.Source))
                     .ForMember(d => d.NestedPropertyValue, o => o.MapFrom(s => s.NestedType.NestedProperty))
                     .ReverseMap();
                });
                Expression expression = config.BuildExecutionPlan(
                    typeof(DestinationType),
                    typeof(SourceType));

                string[] result =
                    this.visitor.GetAssignments(expression)
                        .Select(kvp => kvp.Key.Name)
                        .ToArray();

                result.Should().HaveCount(2).And.BeEquivalentTo(
                    nameof(DestinationType.Destination),
                    nameof(DestinationType.NestedPropertyValue));
            }

            [Fact]
            public void ShouldFindInsideConditionalExpressions()
            {
                Expression expression = MakeAssignment(
                    s => s.Source,
                    d => d != null ? d.Destination : string.Empty);

                IEnumerable<KeyValuePair<MemberInfo, Expression>> result =
                    this.visitor.GetAssignments(expression);

                KeyValuePair<MemberInfo, Expression> kvp = result.Should().ContainSingle().Subject;
                kvp.Key.Name.Should().Be(nameof(DestinationType.Destination));
                kvp.Value.Should().BeAssignableTo<MemberExpression>()
                   .Which.Member.Name.Should().Be(nameof(SourceType.Source));
            }

            [Fact]
            public void ShouldFindNestedProperties()
            {
                Expression expression = MakeAssignment(
                    s => s.NestedType.NestedProperty,
                    d => d.NestedPropertyValue);

                IEnumerable<KeyValuePair<MemberInfo, Expression>> result =
                    this.visitor.GetAssignments(expression);

                KeyValuePair<MemberInfo, Expression> kvp = result.Should().ContainSingle().Subject;
                kvp.Key.Name.Should().Be(nameof(DestinationType.NestedPropertyValue));
                kvp.Value.Should().BeAssignableTo<MemberExpression>()
                   .Which.Member.Name.Should().Be(nameof(NestedType.NestedProperty));
            }

            [Fact]
            public void ShouldFindSimpleAssignments()
            {
                Expression expression = MakeAssignment(
                    s => s.Source,
                    d => d.Destination);

                IEnumerable<KeyValuePair<MemberInfo, Expression>> result =
                    this.visitor.GetAssignments(expression);

                KeyValuePair<MemberInfo, Expression> kvp = result.Should().ContainSingle().Subject;
                kvp.Key.Name.Should().Be(nameof(DestinationType.Destination));
                kvp.Value.Should().BeAssignableTo<MemberExpression>()
                   .Which.Member.Name.Should().Be(nameof(SourceType.Source));
            }

            private static Expression MakeAssignment(
                Expression<Func<SourceType, string>> destination,
                Expression<Func<DestinationType, string>> source)
            {
                return Expression.Assign(destination.Body, source.Body);
            }
        }

        private class DestinationType
        {
            public string Destination { get; set; }
            public string NestedPropertyValue { get; set; }
        }

        private class NestedType
        {
            public string NestedProperty { get; set; }
        }

        private class SourceType
        {
            public NestedType NestedType { get; set; }
            public string Source { get; set; }
        }
    }
}
