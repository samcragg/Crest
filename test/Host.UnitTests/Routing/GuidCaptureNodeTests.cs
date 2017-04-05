namespace Host.UnitTests.Routing
{
    using System;
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class GuidCaptureNodeTests
    {
        private const string ParameterName = "parameter";
        private GuidCaptureNode node;

        [SetUp]
        public void SetUp()
        {
            this.node = new GuidCaptureNode(ParameterName);
        }

        [TestFixture]
        public sealed new class Equals : GuidCaptureNodeTests
        {
            [Test]
            public void ShouldReturnFalseForDifferentParameters()
            {
                var other = new GuidCaptureNode(ParameterName + "New");
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnFalseForNonGuidCaptureNodes()
            {
                IMatchNode other = Substitute.For<IMatchNode>();
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnTrueForTheSameParameter()
            {
                var other = new GuidCaptureNode(ParameterName);
                this.node.Equals(other).Should().BeTrue();
            }
        }

        [TestFixture]
        public sealed class Match : GuidCaptureNodeTests
        {
            // These formats are taken from Guid.ToString https://msdn.microsoft.com/en-us/library/windows/apps/97af8hh4.aspx
            [TestCase("637325b675c145c4aa64d905cf3f7a90")]
            [TestCase("637325b6-75c1-45c4-aa64-d905cf3f7a90")]
            [TestCase("{637325b6-75c1-45c4-aa64-d905cf3f7a90}")]
            [TestCase("(637325b6-75c1-45c4-aa64-d905cf3f7a90)")]
            public void ShouldMatchValidGuidStringFormats(string value)
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("/" + value + "/", 1, value.Length + 1));

                result.Success.Should().BeTrue();
            }

            [TestCase("637325b6-75c1-45c4-aa64d905cf3f7a90")]
            [TestCase("637325b6-75c1-45c4-aa640d905cf3f7a90")]
            [TestCase("637325b6-75c1-45c40aa64-d905cf3f7a90")]
            [TestCase("637325b6-75c1045c4-aa64-d905cf3f7a90")]
            [TestCase("637325b6075c1-45c4-aa64-d905cf3f7a90")]
            [TestCase("{637325b6-75c1-45c4-aa64-d905cf3f7a90)")]
            [TestCase("+637325b6-75c1-45c4-aa64-d905cf3f7a90+")]
            public void ShouldNotMatchInvalidFormattedGuids(string guid)
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment(guid, 0, guid.Length));

                result.Success.Should().BeFalse();
            }

            [Test]
            public void ShouldNotMatchInvalidHexValues()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("/ABCDEFGH-ijkl-MNOP-qrstuvwxyz12/", 1, 37));

                result.Success.Should().BeFalse();
            }

            [Test]
            public void ShouldReturnTheCapturedParameter()
            {
                var guid = new Guid("637325B6-75C1-45C4-AA64-D905CF3F7A90");

                NodeMatchResult result = this.node.Match(
                    new StringSegment("/" + guid.ToString("D") + "/", 1, 37));

                result.Name.Should().Be(ParameterName);
                result.Value.Should().Be(guid);
            }
        }

        [TestFixture]
        public sealed class Priority : GuidCaptureNodeTests
        {
            [Test]
            public void ShouldReturnAPositiveValue()
            {
                this.node.Priority.Should().BePositive();
            }
        }
    }
}
