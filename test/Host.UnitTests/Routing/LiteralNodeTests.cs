namespace Host.UnitTests.Routing
{
    using Crest.Host;
    using Crest.Host.Routing;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class LiteralNodeTests
    {
        private const string LiteralString = "literal";
        private LiteralNode node;

        [SetUp]
        public void SetUp()
        {
            this.node = new LiteralNode(LiteralString);
        }

        [Test]
        public void PriorityShouldReturnAPositiveValue()
        {
            Assert.That(this.node.Priority, Is.Positive);
        }

        [Test]
        public void EqualsShouldReturnFalseForNonLiteralNodes()
        {
            IMatchNode other = Substitute.For<IMatchNode>();
            Assert.That(this.node.Equals(other), Is.False);
        }

        [Test]
        public void EqualsShouldReturnFalseForDifferentLiterals()
        {
            var other = new LiteralNode(LiteralString + "New");
            Assert.That(this.node.Equals(other), Is.False);
        }

        [Test]
        public void EqualsShouldReturnTrueForTheSameLiteral()
        {
            var other = new LiteralNode(LiteralString);
            Assert.That(this.node.Equals(other), Is.True);
        }

        [Test]
        public void EqualsShouldIgnoreTheCaseOfTheLiteral()
        {
            var other = new LiteralNode(LiteralString.ToUpperInvariant());
            Assert.That(this.node.Equals(other), Is.True);
        }

        [Test]
        public void MatchShouldReturnUnsuccessfulIfTheLiteralIsNotAtTheSpecifiedLocation()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("not_here_literal", 0, 16));

            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void MatchShouldReturnSuccessIfTheLiteralIfTheSubstringMatchesTheLiteral()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("ignore_" + LiteralString, "ignore_".Length, "ignore_".Length + LiteralString.Length));

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void MatchShouldIgnoreTheCaseWhenComparing()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment(LiteralString.ToUpperInvariant(), 0, LiteralString.Length));

            Assert.That(result.Success, Is.True);
        }
    }
}
