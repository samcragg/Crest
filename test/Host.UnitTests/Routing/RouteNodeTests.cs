namespace Host.UnitTests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class RouteNodeTests
    {
        private const string MatcherValue = "Stored value";
        private readonly IMatchNode matcher;
        private readonly RouteNode<string> node;

        public RouteNodeTests()
        {
            this.matcher = Substitute.For<IMatchNode>();
            this.matcher.Match(Arg.Any<StringSegment>())
                .Returns(new NodeMatchResult(string.Empty, null));

            this.node = new RouteNode<string>(this.matcher) { Value = MatcherValue };
        }

        public sealed class Add : RouteNodeTests
        {
            [Fact]
            public void ShouldCombineMatchers()
            {
                IMatchNode child = Substitute.For<IMatchNode>();
                IMatchNode duplicate = Substitute.For<IMatchNode>();
                child.Equals(duplicate).Returns(true);

                this.node.Add(new[] { child }, 0);
                this.node.Add(new[] { duplicate }, 0);
                this.node.Match("/matcher_part/child_part");

                child.ReceivedWithAnyArgs().Match(default);
                duplicate.DidNotReceiveWithAnyArgs().Match(default);
            }

            [Fact]
            public void ShouldReturnTheLeafNode()
            {
                IMatchNode first = Substitute.For<IMatchNode>();
                IMatchNode second = Substitute.For<IMatchNode>();

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
                    new[] { Substitute.For<IMatchNode>() },
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
                IMatchNode normal = Substitute.For<IMatchNode>();
                normal.Priority.Returns(100);
                this.node.Add(new[] { normal }, 0);

                IMatchNode important = Substitute.For<IMatchNode>();
                important.Priority.Returns(200);
                important.Match(default)
                         .ReturnsForAnyArgs(new NodeMatchResult(string.Empty, null));
                this.node.Add(new[] { important }, 0);

                this.node.Match("/matcher/part");

                important.ReceivedWithAnyArgs().Match(default);
                normal.DidNotReceiveWithAnyArgs().Match(default);
            }

            [Fact]
            public void ShouldReturnTheCapturedValues()
            {
                this.matcher.Match(default)
                    .ReturnsForAnyArgs(new NodeMatchResult("parameter", 123));

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
    }
}
