namespace Host.UnitTests.Routing
{
    using System.Linq;
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class RouteNodeTests
    {
        private IMatchNode matcher;
        private string MatcherValue = "Stored value";
        private RouteNode<string> node;

        [SetUp]
        public void SetUp()
        {
            this.matcher = Substitute.For<IMatchNode>();
            this.matcher.Match(Arg.Any<StringSegment>())
                .Returns(new NodeMatchResult(null, null));

            this.node = new RouteNode<string>(this.matcher) { Value = MatcherValue };
        }

        [TestFixture]
        public sealed class Add : RouteNodeTests
        {
            [Test]
            public void ShouldCombineMatchers()
            {
                IMatchNode child = Substitute.For<IMatchNode>();
                IMatchNode duplicate = Substitute.For<IMatchNode>();
                child.Equals(duplicate).Returns(true);

                this.node.Add(new[] { child }, 0);
                this.node.Add(new[] { duplicate }, 0);
                this.node.Match("/matcher_part/child_part");

                child.ReceivedWithAnyArgs().Match(default(StringSegment));
                duplicate.DidNotReceiveWithAnyArgs().Match(default(StringSegment));
            }

            [Test]
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

        [TestFixture]
        public sealed class Match : RouteNodeTests
        {
            [Test]
            public void ShouldInvokeHigherPriorityNodesFirst()
            {
                IMatchNode normal = Substitute.For<IMatchNode>();
                normal.Priority.Returns(100);
                this.node.Add(new[] { normal }, 0);

                IMatchNode important = Substitute.For<IMatchNode>();
                important.Priority.Returns(200);
                important.Match(default(StringSegment))
                         .ReturnsForAnyArgs(new NodeMatchResult(null, null));
                this.node.Add(new[] { important }, 0);

                this.node.Match("/matcher/part");

                important.ReceivedWithAnyArgs().Match(default(StringSegment));
                normal.DidNotReceiveWithAnyArgs().Match(default(StringSegment));
            }

            [Test]
            public void ShouldReturnTheCapturedValues()
            {
                this.matcher.Match(default(StringSegment))
                    .ReturnsForAnyArgs(new NodeMatchResult("parameter", 123));

                RouteNode<string>.MatchResult result = this.node.Match("/route");

                result.Captures.Keys.Single().Should().Be("parameter");
                result.Captures.Values.Single().Should().Be(123);
            }

            [Test]
            public void ShouldReturnTheValueSavedAgainstTheNode()
            {
                this.matcher.Match(default(StringSegment))
                    .ReturnsForAnyArgs(new NodeMatchResult(null, null));

                RouteNode<string>.MatchResult result = this.node.Match("/route");

                result.Success.Should().BeTrue();
                result.Value.Should().Be(MatcherValue);
            }
        }
    }
}
