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
            this.node = new RouteNode<string>(null);
            this.node.Add(new[] { this.matcher }, 0, MatcherValue);
        }

        [Test]
        public void AddShouldCombineMatchers()
        {
            // We need the child node so we can insert the value somewhere,
            // otherwise it will try to overwrite the value in this.matcher,
            // which isn't allowed
            IMatchNode duplicate = Substitute.For<IMatchNode>();
            IMatchNode child = Substitute.For<IMatchNode>();
            this.matcher.Equals(duplicate).Returns(true);

            this.node.Add(new[] { duplicate, child }, 0, null);
            this.node.Match("/route");

            this.matcher.ReceivedWithAnyArgs().Match(default(StringSegment));
            duplicate.DidNotReceiveWithAnyArgs().Match(default(StringSegment));
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
            this.matcher.Priority.Returns(100);
            IMatchNode important = Substitute.For<IMatchNode>();
            important.Priority.Returns(200);
            important.Match(default(StringSegment))
                     .ReturnsForAnyArgs(new NodeMatchResult(null, null));
            this.node.Add(new[] { important }, 0, null);

            this.node.Match("/route");

            important.ReceivedWithAnyArgs().Match(default(StringSegment));
            this.matcher.DidNotReceiveWithAnyArgs().Match(default(StringSegment));
        }
    }
}
