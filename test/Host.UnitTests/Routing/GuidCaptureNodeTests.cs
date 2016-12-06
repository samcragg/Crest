namespace Host.UnitTests.Routing
{
    using System;
    using Crest.Host;
    using Crest.Host.Routing;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class GuidCaptureNodeTests
    {
        private const string ParameterName = "parameter";
        private GuidCaptureNode node;

        [SetUp]
        public void SetUp()
        {
            this.node = new GuidCaptureNode(ParameterName);
        }

        [Test]
        public void PriorityShouldReturnAPositiveValue()
        {
            Assert.That(this.node.Priority, Is.Positive);
        }

        [Test]
        public void EqualsShouldReturnFalseForNonGuidCaptureNodes()
        {
            IMatchNode other = Substitute.For<IMatchNode>();
            Assert.That(this.node.Equals(other), Is.False);
        }

        [Test]
        public void EqualsShouldReturnFalseForDifferentParameters()
        {
            var other = new GuidCaptureNode(ParameterName + "New");
            Assert.That(this.node.Equals(other), Is.False);
        }

        [Test]
        public void EqualsShouldReturnTrueForTheSameParameter()
        {
            var other = new GuidCaptureNode(ParameterName);
            Assert.That(this.node.Equals(other), Is.True);
        }

        [TestCase("637325b6-75c1-45c4-aa64d905cf3f7a90")]
        [TestCase("637325b6-75c1-45c4-aa640d905cf3f7a90")]
        [TestCase("637325b6-75c1-45c40aa64-d905cf3f7a90")]
        [TestCase("637325b6-75c1045c4-aa64-d905cf3f7a90")]
        [TestCase("637325b6075c1-45c4-aa64-d905cf3f7a90")]
        [TestCase("{637325b6-75c1-45c4-aa64-d905cf3f7a90)")]
        [TestCase("+637325b6-75c1-45c4-aa64-d905cf3f7a90+")]
        public void MatchShouldNotMatchInvalidFormattedGuids(string guid)
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment(guid, 0, guid.Length));

            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void MatchShouldNotMatchInvalidHexValues()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("/ABCDEFGH-ijkl-MNOP-qrstuvwxyz12/", 1, 37));

            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void MatchShouldReturnTheCapturedParameter()
        {
            var guid = new Guid("637325B6-75C1-45C4-AA64-D905CF3F7A90");

            NodeMatchResult result = this.node.Match(
                new StringSegment("/" + guid.ToString("D") + "/", 1, 37));

            Assert.That(result.Name, Is.EqualTo(ParameterName));
            Assert.That(result.Value, Is.EqualTo(guid));
        }

        // These formats are taken from Guid.ToString https://msdn.microsoft.com/en-us/library/windows/apps/97af8hh4.aspx

        [Test]
        public void MatchShouldMatchGuidsWithoutHyphens()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("/637325b675c145c4aa64d905cf3f7a90/", 1, 33));

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void MatchShouldMatchGuidsWithHyphens()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("/637325b6-75c1-45c4-aa64-d905cf3f7a90/", 1, 37));

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void MatchShouldMatchGuidsEnclosedInBrackets()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("/{637325b6-75c1-45c4-aa64-d905cf3f7a90}/", 1, 39));

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void MatchShouldMatchGuidsWnclosesInParentheses()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("/(637325b6-75c1-45c4-aa64-d905cf3f7a90)/", 1, 39));

            Assert.That(result.Success, Is.True);
        }
    }
}
