namespace Host.UnitTests.Routing
{
    using Crest.Host;
    using Crest.Host.Routing;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class VersionCaptureNodeTests
    {
        private VersionCaptureNode node;

        [SetUp]
        public void SetUp()
        {
            this.node = new VersionCaptureNode();
        }

        [Test]
        public void PriorityShouldReturnAPositiveValue()
        {
            Assert.That(this.node.Priority, Is.Positive);
        }

        [Test]
        public void EqualsShouldReturnFalseForNonVersionCaptureNodes()
        {
            IMatchNode other = Substitute.For<IMatchNode>();
            Assert.That(this.node.Equals(other), Is.False);
        }

        [Test]
        public void EqualsShouldReturnTrueForOtherVersionCaptureNodes()
        {
            var other = new VersionCaptureNode();
            Assert.That(this.node.Equals(other), Is.True);
        }

        [Test]
        public void MatchShouldMatchAnyCase()
        {
            NodeMatchResult lower = this.node.Match(
                new StringSegment("/v1/", 1, 3));

            NodeMatchResult upper = this.node.Match(
                new StringSegment("/V1/", 1, 3));

            Assert.That(lower.Success, Is.True);
            Assert.That(upper.Success, Is.True);
        }

        [Test]
        public void MatchShouldSaveTheVersionNumber()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("v12", 0, 3));

            Assert.That(result.Name, Is.EqualTo(VersionCaptureNode.KeyName));
            Assert.That(result.Value, Is.EqualTo(12));
        }

        [TestCase("v")]
        [TestCase("x10")]
        [TestCase("vNext")]
        public void MatchShouldNotMatchInvalidVersions(string version)
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment(version, 0, version.Length));

            Assert.That(result.Success, Is.False);
        }
    }
}
