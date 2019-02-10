namespace Host.UnitTests.Routing.Captures
{
    using System;
    using Crest.Host.Routing;
    using Crest.Host.Routing.Captures;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class VersionCaptureNodeTests
    {
        private readonly VersionCaptureNode node = new VersionCaptureNode();

        public new sealed class Equals : VersionCaptureNodeTests
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
                NodeMatchInfo lower = this.node.Match("v1".AsSpan());
                NodeMatchInfo upper = this.node.Match("V1".AsSpan());

                lower.Success.Should().BeTrue();
                upper.Success.Should().BeTrue();
            }

            [Theory]
            [InlineData("v")]
            [InlineData("x10")]
            [InlineData("vNext")]
            public void ShouldNotMatchInvalidVersions(string version)
            {
                NodeMatchInfo result = this.node.Match(version.AsSpan());

                result.Success.Should().BeFalse();
            }

            [Fact]
            public void ShouldSaveTheVersionNumber()
            {
                NodeMatchInfo result = this.node.Match("v12".AsSpan());

                result.Parameter.Should().Be(VersionCaptureNode.KeyName);
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
