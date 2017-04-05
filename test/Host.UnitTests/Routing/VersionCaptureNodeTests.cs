namespace Host.UnitTests.Routing
{
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class VersionCaptureNodeTests
    {
        private VersionCaptureNode node;

        [SetUp]
        public void SetUp()
        {
            this.node = new VersionCaptureNode();
        }

        [TestFixture]
        public sealed new class Equals : VersionCaptureNodeTests
        {
            [Test]
            public void ShouldReturnFalseForNonVersionCaptureNodes()
            {
                IMatchNode other = Substitute.For<IMatchNode>();
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnTrueForOtherVersionCaptureNodes()
            {
                var other = new VersionCaptureNode();
                this.node.Equals(other).Should().BeTrue();
            }
        }

        [TestFixture]
        public sealed class Match : VersionCaptureNodeTests
        {
            [Test]
            public void ShouldMatchAnyCase()
            {
                NodeMatchResult lower = this.node.Match(
                    new StringSegment("/v1/", 1, 3));

                NodeMatchResult upper = this.node.Match(
                    new StringSegment("/V1/", 1, 3));

                lower.Success.Should().BeTrue();
                upper.Success.Should().BeTrue();
            }

            [TestCase("v")]
            [TestCase("x10")]
            [TestCase("vNext")]
            public void ShouldNotMatchInvalidVersions(string version)
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment(version, 0, version.Length));

                result.Success.Should().BeFalse();
            }

            [Test]
            public void ShouldSaveTheVersionNumber()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("v12", 0, 3));

                result.Name.Should().Be(VersionCaptureNode.KeyName);
                result.Value.Should().Be(12);
            }
        }

        [TestFixture]
        public sealed class Priority : VersionCaptureNodeTests
        {
            [Test]
            public void ShouldReturnAPositiveValue()
            {
                this.node.Priority.Should().BePositive();
            }
        }
    }
}
