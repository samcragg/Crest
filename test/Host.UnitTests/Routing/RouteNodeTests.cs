namespace Host.UnitTests.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.Routing;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class RouteNodeTests
    {
        private RouteMethod matcherValue;
        private IMatchNode matcher;
        private RouteNode node;

        [SetUp]
        public void SetUp()
        {
            this.matcher = Substitute.For<IMatchNode>();
            this.matcherValue = Method;
            this.node = new RouteNode(null);
            this.node.Add(new[] { this.matcher }, 0, this.matcherValue);
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

            RouteNode.MatchResult result = this.node.Match("/route");

            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.EqualTo(this.matcherValue));
        }

        [Test]
        public void MatchShouldReturnTheCapturedValues()
        {
            this.matcher.Match(default(StringSegment))
                .ReturnsForAnyArgs(new NodeMatchResult("parameter", 123));

            RouteNode.MatchResult result = this.node.Match("/route");

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

        private static Task<object> Method(IReadOnlyDictionary<string, object> parameters)
        {
            return null;
        }
    }
}
