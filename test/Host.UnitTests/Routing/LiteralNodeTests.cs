namespace Host.UnitTests.Routing
{
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class LiteralNodeTests
    {
        private const string LiteralString = "literal";
        private LiteralNode node;

        [SetUp]
        public void SetUp()
        {
            this.node = new LiteralNode(LiteralString);
        }

        [TestFixture]
        public sealed new class Equals : LiteralNodeTests
        {
            [Test]
            public void ShouldIgnoreTheCaseOfTheLiteral()
            {
                var other = new LiteralNode(LiteralString.ToUpperInvariant());
                this.node.Equals(other).Should().BeTrue();
            }

            [Test]
            public void ShouldReturnFalseForDifferentParameters()
            {
                var other = new LiteralNode(LiteralString + "New");
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnFalseForNonLiteralNodes()
            {
                IMatchNode other = Substitute.For<IMatchNode>();
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnTrueForTheSameParameter()
            {
                var other = new LiteralNode(LiteralString);
                this.node.Equals(other).Should().BeTrue();
            }
        }

        [TestFixture]
        public sealed class Match : LiteralNodeTests
        {
            [Test]
            public void ShouldIgnoreTheCaseWhenComparing()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment(LiteralString.ToUpperInvariant(), 0, LiteralString.Length));

                result.Success.Should().BeTrue();
            }

            [Test]
            public void ShouldReturnSuccessIfTheLiteralIfTheSubstringMatchesTheLiteral()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("ignore_" + LiteralString, "ignore_".Length, "ignore_".Length + LiteralString.Length));

                result.Success.Should().BeTrue();
            }

            [Test]
            public void ShouldReturnUnsuccessfulIfTheLiteralIsNotAtTheSpecifiedLocation()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("not_here_literal", 0, 16));

                result.Success.Should().BeFalse();
            }
        }

        [TestFixture]
        public sealed class Priority : LiteralNodeTests
        {
            [Test]
            public void ShouldReturnAPositiveValue()
            {
                this.node.Priority.Should().BePositive();
            }
        }
    }
}
