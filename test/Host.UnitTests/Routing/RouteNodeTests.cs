namespace Host.UnitTests.Routing
{
    using System.Linq;
    using Crest.Host;
    using Crest.Host.Routing;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RouteNodeTests
    {
        private string MatcherValue = "Stored value";
        private IMatchNode matcher;
        private RouteNode<string> node;

        [SetUp]
        public void SetUp()
        {
            this.matcher = Substitute.For<IMatchNode>();
            this.matcher.Match(Arg.Any<StringSegment>())
                .Returns(new NodeMatchResult(null, null));

            this.node = new RouteNode<string>(this.matcher) { Value = MatcherValue };
        }

        [Test]
        public void AddShouldCombineMatchers()
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
        public void AddShouldReturnTheLeafNode()
        {
            IMatchNode first = Substitute.For<IMatchNode>();
            IMatchNode second = Substitute.For<IMatchNode>();

            RouteNode<string> node1 = this.node.Add(new[] { first }, 0);
            RouteNode<string> node2 = this.node.Add(new[] { first, second }, 0);

            Assert.That(node1, Is.Not.EqualTo(this.node));
            Assert.That(node2, Is.Not.EqualTo(node1));
        }

        [Test]
        public void MatchShouldReturnTheValueSavedAgainstTheNode()
        {
            this.matcher.Match(default(StringSegment))
                .ReturnsForAnyArgs(new NodeMatchResult(null, null));

            RouteNode<string>.MatchResult result = this.node.Match("/route");

            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.EqualTo(MatcherValue));
        }

        [Test]
        public void MatchShouldReturnTheCapturedValues()
        {
            this.matcher.Match(default(StringSegment))
                .ReturnsForAnyArgs(new NodeMatchResult("parameter", 123));

            RouteNode<string>.MatchResult result = this.node.Match("/route");

            Assert.That(result.Captures.Keys.Single(), Is.EqualTo("parameter"));
            Assert.That(result.Captures.Values.Single(), Is.EqualTo(123));
        }

        [Test]
        public void MatchShouldInvokeHigherPriorityNodesFirst()
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
    }
}
