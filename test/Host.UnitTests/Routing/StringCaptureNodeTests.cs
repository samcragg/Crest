namespace Host.UnitTests.Routing
{
    using Crest.Host;
    using Crest.Host.Routing;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class StringCaptureNodeTests
    {
        private const string ParameterName = "parameter";
        private StringCaptureNode node;

        [SetUp]
        public void SetUp()
        {
            this.node = new StringCaptureNode(ParameterName);
        }

        [Test]
        public void PriorityShouldReturnAPositiveValue()
        {
            Assert.That(this.node.Priority, Is.Positive);
        }

        [Test]
        public void EqualsShouldReturnFalseForNonStringCaptureNodes()
        {
            IMatchNode other = Substitute.For<IMatchNode>();
            Assert.That(this.node.Equals(other), Is.False);
        }

        [Test]
        public void EqualsShouldReturnFalseForDifferentParameters()
        {
            var other = new StringCaptureNode(ParameterName + "New");
            Assert.That(this.node.Equals(other), Is.False);
        }

        [Test]
        public void EqualsShouldReturnTrueForTheSameParameter()
        {
            var other = new StringCaptureNode(ParameterName);
            Assert.That(this.node.Equals(other), Is.True);
        }

        [Test]
        public void MatchShouldMatchAnyString()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("/string/", 1, 7));

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void MatchShouldSaveTheCapturedParameter()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("01234", 2, 4));

            Assert.That(result.Name, Is.EqualTo(ParameterName));
            Assert.That(result.Value, Is.EqualTo("23"));
        }
    }
}
