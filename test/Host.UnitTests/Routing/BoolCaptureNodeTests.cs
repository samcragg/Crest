namespace Host.UnitTests.Routing
{
    using Crest.Host;
    using Crest.Host.Routing;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class BoolCaptureNodeTests
    {
        private const string ParameterName = "parameter";
        private BoolCaptureNode node;

        [SetUp]
        public void SetUp()
        {
            this.node = new BoolCaptureNode(ParameterName);
        }

        [Test]
        public void PriorityShouldReturnAPositiveValue()
        {
            Assert.That(this.node.Priority, Is.Positive);
        }

        [Test]
        public void EqualsShouldReturnFalseForNonBoolCaptureNodes()
        {
            IMatchNode other = Substitute.For<IMatchNode>();
            Assert.That(this.node.Equals(other), Is.False);
        }

        [Test]
        public void EqualsShouldReturnFalseForDifferentParameters()
        {
            var other = new BoolCaptureNode(ParameterName + "New");
            Assert.That(this.node.Equals(other), Is.False);
        }

        [Test]
        public void EqualsShouldReturnTrueForTheSameParameter()
        {
            var other = new BoolCaptureNode(ParameterName);
            Assert.That(this.node.Equals(other), Is.True);
        }

        [Test]
        public void MatchShouldMatchFalse()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("/False/", 1, 6));

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void MatchShouldMatchTrue()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("/True/", 1, 5));

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void MatchShouldNotMatchPartialWords()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("_true_", 1, 4));

            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void MatchShouldReturnTheCapturedParameter()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("true", 0, 4));

            Assert.That(result.Name, Is.EqualTo(ParameterName));
        }

        [Test]
        public void MatchShouldMatchZeroAsFalse()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("_0_", 1, 2));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.False);
        }

        [Test]
        public void MatchShouldMatchOneAsTrue()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("_1_", 1, 2));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.True);
        }
    }
}
