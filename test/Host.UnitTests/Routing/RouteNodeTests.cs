namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Host.Routing;
    using FluentAssertions;
    using Xunit;

    public class RouteNodeTests
    {
        private const string MatcherValue = "Stored value";
        private readonly FakeMatchNode matcher;
        private readonly RouteNode<string> node;

        public RouteNodeTests()
        {
            this.matcher = new FakeMatchNode();
            this.node = new RouteNode<string>(this.matcher) { Value = MatcherValue };
        }

        public sealed class Add : RouteNodeTests
        {
            [Fact]
            public void ShouldCombineMatchers()
            {
                var child = new FakeMatchNode();
                var duplicate = new FakeMatchNode();
                child.EqualsMethod = other => other == duplicate;

                this.node.Add(new[] { child }, 0);
                this.node.Add(new[] { duplicate }, 0);
                this.node.Match("/matcher_part/child_part");

                child.MatchSegment.Should().NotBeNull();
                duplicate.MatchSegment.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheLeafNode()
            {
                IMatchNode first = new FakeMatchNode();
                IMatchNode second = new FakeMatchNode();

                RouteNode<string> node1 = this.node.Add(new[] { first }, 0);
                RouteNode<string> node2 = this.node.Add(new[] { first, second }, 0);

                node1.Should().NotBe(this.node);
                node2.Should().NotBe(node1);
            }
        }

        public sealed class Flatten : RouteNodeTests
        {
            [Fact]
            public void ShouldReturnChildren()
            {
                RouteNode<string> child = this.node.Add(
                    new[] { new FakeMatchNode() },
                    0);

                IEnumerable<RouteNode<string>> result = this.node.Flatten();

                result.Should().Contain(child);
            }

            [Fact]
            public void ShouldReturnItself()
            {
                IEnumerable<RouteNode<string>> result = this.node.Flatten();

                result.Should().Contain(this.node);
            }
        }

        public sealed class Match : RouteNodeTests
        {
            [Fact]
            public void ShouldInvokeHigherPriorityNodesFirst()
            {
                var normal = new FakeMatchNode { Priority = 100 };
                this.node.Add(new[] { normal }, 0);

                var important = new FakeMatchNode { Priority = 200 };
                this.node.Add(new[] { important }, 0);

                this.node.Match("/matcher/part");

                important.MatchSegment.Should().NotBeNull();
                normal.MatchSegment.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheCapturedValues()
            {
                this.matcher.MatchResult = new NodeMatchResult("parameter", 123);

                RouteNode<string>.MatchResult result = this.node.Match("/route");

                result.Captures.Keys.Single().Should().Be("parameter");
                result.Captures.Values.Single().Should().Be(123);
            }

            [Fact]
            public void ShouldReturnTheValueSavedAgainstTheNode()
            {
                RouteNode<string>.MatchResult result = this.node.Match("/route");

                result.Success.Should().BeTrue();
                result.Value.Should().Be(MatcherValue);
            }
        }

        private class FakeMatchNode : IMatchNode
        {
            public int Priority { get; set; }

            public string ParameterName => string.Empty;

            internal Func<IMatchNode, bool> EqualsMethod { get; set; } = _ => false;

            internal NodeMatchResult MatchResult { get; set; }
                = new NodeMatchResult(string.Empty, null);

            internal string MatchSegment { get; private set; }

            public bool Equals(IMatchNode other)
            {
                return this.EqualsMethod(other);
            }

            public NodeMatchResult Match(ReadOnlySpan<char> segment)
            {
                this.MatchSegment = segment.ToString();
                return this.MatchResult;
            }

            public bool TryConvertValue(ReadOnlySpan<char> value, out object result)
            {
                result = null;
                return false;
            }
        }
    }
}
