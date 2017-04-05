namespace Host.UnitTests.Routing
{
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class BoolCaptureNodeTests
    {
        private const string ParameterName = "parameter";
        private BoolCaptureNode node;

        [SetUp]
        public void SetUp()
        {
            this.node = new BoolCaptureNode(ParameterName);
        }

        [TestFixture]
        public sealed new class Equals : BoolCaptureNodeTests
        {
            [Test]
            public void ShouldReturnFalseForDifferentParameters()
            {
                var other = new BoolCaptureNode(ParameterName + "New");
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnFalseForNonBoolCaptureNodes()
            {
                IMatchNode other = Substitute.For<IMatchNode>();
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnTrueForTheSameParameter()
            {
                var other = new BoolCaptureNode(ParameterName);
                this.node.Equals(other).Should().BeTrue();
            }
        }

        [TestFixture]
        public sealed class Match : BoolCaptureNodeTests
        {
            [Test]
            public void ShouldMatchFalse()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("/False/", 1, 6));

                result.Success.Should().BeTrue();
            }

            [Test]
            public void ShouldMatchOneAsTrue()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("_1_", 1, 2));

                result.Success.Should().BeTrue();
                result.Value.Should().Be(true);
            }

            [Test]
            public void ShouldMatchTrue()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("/True/", 1, 5));

                result.Success.Should().BeTrue();
            }

            [Test]
            public void ShouldMatchZeroAsFalse()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("_0_", 1, 2));

                result.Success.Should().BeTrue();
                result.Value.Should().Be(false);
            }

            [Test]
            public void ShouldNotMatchPartialWords()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("_true_", 1, 4));

                result.Success.Should().BeFalse();
            }

            [Test]
            public void ShouldReturnTheCapturedParameter()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("true", 0, 4));

                result.Name.Should().Be(ParameterName);
            }
        }

        [TestFixture]
        public sealed class Priority : BoolCaptureNodeTests
        {
            [Test]
            public void ShouldReturnAPositiveValue()
            {
                this.node.Priority.Should().BePositive();
            }
        }
    }
}
