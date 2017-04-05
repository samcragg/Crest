namespace Host.UnitTests.Routing
{
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class StringCaptureNodeTests
    {
        private const string ParameterName = "parameter";
        private StringCaptureNode node;

        [SetUp]
        public void SetUp()
        {
            this.node = new StringCaptureNode(ParameterName);
        }

        [TestFixture]
        public sealed new class Equals : StringCaptureNodeTests
        {
            [Test]
            public void ShouldReturnFalseForDifferentParameters()
            {
                var other = new StringCaptureNode(ParameterName + "New");
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnFalseForNonStringCaptureNodes()
            {
                IMatchNode other = Substitute.For<IMatchNode>();
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnTrueForTheSameParameter()
            {
                var other = new StringCaptureNode(ParameterName);
                this.node.Equals(other).Should().BeTrue();
            }
        }

        [TestFixture]
        public sealed class Match : StringCaptureNodeTests
        {
            [Test]
            public void ShouldMatchAnyString()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("/string/", 1, 7));

                result.Success.Should().BeTrue();
            }

            [Test]
            public void ShouldSaveTheCapturedParameter()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("01234", 2, 4));

                result.Name.Should().Be(ParameterName);
                result.Value.Should().Be("23");
            }
        }

        [TestFixture]
        public sealed class Priority : StringCaptureNodeTests
        {
            [Test]
            public void ShouldReturnAPositiveValue()
            {
                this.node.Priority.Should().BePositive();
            }
        }
    }
}
