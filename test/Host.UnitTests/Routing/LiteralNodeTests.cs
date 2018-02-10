namespace Host.UnitTests.Routing
{
    using System;
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class LiteralNodeTests
    {
        private const string LiteralString = "literal";
        private readonly LiteralNode node = new LiteralNode(LiteralString);

        public sealed new class Equals : LiteralNodeTests
        {
            [Fact]
            public void ShouldIgnoreTheCaseOfTheLiteral()
            {
                var other = new LiteralNode(LiteralString.ToUpperInvariant());
                this.node.Equals(other).Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnFalseForDifferentParameters()
            {
                var other = new LiteralNode(LiteralString + "New");
                this.node.Equals(other).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForNonLiteralNodes()
            {
                IMatchNode other = Substitute.For<IMatchNode>();
                this.node.Equals(other).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueForTheSameParameter()
            {
                var other = new LiteralNode(LiteralString);
                this.node.Equals(other).Should().BeTrue();
            }
        }

        public sealed class Match : LiteralNodeTests
        {
            [Fact]
            public void ShouldIgnoreTheCaseWhenComparing()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment(LiteralString.ToUpperInvariant()));

                result.Success.Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnSuccessIfTheLiteralIfTheSubstringMatchesTheLiteral()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("ignore_" + LiteralString, "ignore_".Length, "ignore_".Length + LiteralString.Length));

                result.Success.Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnUnsuccessfulIfTheLiteralIsNotAtTheSpecifiedLocation()
            {
                NodeMatchResult result = this.node.Match(new StringSegment("not_here_literal"));

                result.Success.Should().BeFalse();
            }
        }

        public sealed class ParameterName : LiteralNodeTests
        {
            [Fact]
            public void ShouldThrowNotSupportedException()
            {
                this.node.Invoking<IQueryValueConverter>(x => _ = x.ParameterName)
                    .Should().Throw<NotSupportedException>();
            }
        }

        public sealed class Priority : LiteralNodeTests
        {
            [Fact]
            public void ShouldReturnAPositiveValue()
            {
                this.node.Priority.Should().BePositive();
            }
        }

        public sealed class TryConvertValue : LiteralNodeTests
        {
            [Fact]
            public void ShouldThrowNotSupportedException()
            {
                this.node.Invoking<IQueryValueConverter>(x => x.TryConvertValue(default, out _))
                    .Should().Throw<NotSupportedException>();
            }
        }
    }
}
