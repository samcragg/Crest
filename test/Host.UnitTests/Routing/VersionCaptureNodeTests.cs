namespace Host.UnitTests.Routing
{
    using System;
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class VersionCaptureNodeTests
    {
        private readonly VersionCaptureNode node = new VersionCaptureNode();

        public sealed new class Equals : VersionCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnFalseForNonVersionCaptureNodes()
            {
                IMatchNode other = Substitute.For<IMatchNode>();
                this.node.Equals(other).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueForOtherVersionCaptureNodes()
            {
                var other = new VersionCaptureNode();
                this.node.Equals(other).Should().BeTrue();
            }
        }

        public sealed class Match : VersionCaptureNodeTests
        {
            [Fact]
            public void ShouldMatchAnyCase()
            {
                NodeMatchResult lower = this.node.Match(
                    new StringSegment("/v1/", 1, 3));

                NodeMatchResult upper = this.node.Match(
                    new StringSegment("/V1/", 1, 3));

                lower.Success.Should().BeTrue();
                upper.Success.Should().BeTrue();
            }

            [Theory]
            [InlineData("v")]
            [InlineData("x10")]
            [InlineData("vNext")]
            public void ShouldNotMatchInvalidVersions(string version)
            {
                NodeMatchResult result = this.node.Match(new StringSegment(version));

                result.Success.Should().BeFalse();
            }

            [Fact]
            public void ShouldSaveTheVersionNumber()
            {
                NodeMatchResult result = this.node.Match(new StringSegment("v12"));

                result.Name.Should().Be(VersionCaptureNode.KeyName);
                result.Value.Should().Be(12);
            }
        }

        public sealed class ParameterName : VersionCaptureNodeTests
        {
            [Fact]
            public void ShouldThrowNotSupportedException()
            {
                this.node.Invoking<IQueryValueConverter>(x => _ = x.ParameterName)
                    .Should().Throw<NotSupportedException>();
            }
        }

        public sealed class Priority : VersionCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnAPositiveValue()
            {
                this.node.Priority.Should().BePositive();
            }
        }

        public sealed class TryConvertValue : VersionCaptureNodeTests
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
