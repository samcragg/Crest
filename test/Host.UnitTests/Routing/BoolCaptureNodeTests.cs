namespace Host.UnitTests.Routing
{
    using System;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class BoolCaptureNodeTests
    {
        private const string Parameter = "parameter";
        private readonly BoolCaptureNode node = new BoolCaptureNode(Parameter);

        public new sealed class Equals : BoolCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnFalseForDifferentParameters()
            {
                var other = new BoolCaptureNode(Parameter + "New");
                this.node.Equals(other).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForNonBoolCaptureNodes()
            {
                IMatchNode other = Substitute.For<IMatchNode>();
                this.node.Equals(other).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueForTheSameParameter()
            {
                var other = new BoolCaptureNode(Parameter);
                this.node.Equals(other).Should().BeTrue();
            }
        }

        public sealed class Match : BoolCaptureNodeTests
        {
            [Fact]
            public void ShouldMatchFalse()
            {
                NodeMatchResult result = this.node.Match("False".AsSpan());

                result.Success.Should().BeTrue();
            }

            [Fact]
            public void ShouldMatchOneAsTrue()
            {
                NodeMatchResult result = this.node.Match("1".AsSpan());

                result.Success.Should().BeTrue();
                result.Value.Should().Be(true);
            }

            [Fact]
            public void ShouldMatchTrue()
            {
                NodeMatchResult result = this.node.Match("True".AsSpan());

                result.Success.Should().BeTrue();
            }

            [Fact]
            public void ShouldMatchZeroAsFalse()
            {
                NodeMatchResult result = this.node.Match("0".AsSpan());

                result.Success.Should().BeTrue();
                result.Value.Should().Be(false);
            }

            [Fact]
            public void ShouldNotMatchPartialWords()
            {
                NodeMatchResult result = this.node.Match("tru".AsSpan());

                result.Success.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTheCapturedParameter()
            {
                NodeMatchResult result = this.node.Match("true".AsSpan());

                result.Name.Should().Be(Parameter);
            }
        }

        public sealed class ParameterName : BoolCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnTheValuePassedInTheConstructor()
            {
                this.node.ParameterName.Should().Be(Parameter);
            }
        }

        public sealed class Priority : BoolCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnAPositiveValue()
            {
                this.node.Priority.Should().BePositive();
            }
        }

        public sealed class TryConvertValue : BoolCaptureNodeTests
        {
            [Fact]
            public void ShouldMatchFalse()
            {
                bool result = this.node.TryConvertValue(
                    "false".AsSpan(),
                    out object value);

                result.Should().BeTrue();
                value.Should().Be(false);
            }

            [Fact]
            public void ShouldMatchTrue()
            {
                bool result = this.node.TryConvertValue(
                    "true".AsSpan(),
                    out object value);

                result.Should().BeTrue();
                value.Should().Be(true);
            }

            [Fact]
            public void ShouldReturnFalseForInvalidValues()
            {
                bool result = this.node.TryConvertValue(
                    "invalid".AsSpan(),
                    out object value);

                result.Should().BeFalse();
                value.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTrueForEmptyValues()
            {
                bool result = this.node.TryConvertValue(
                    ReadOnlySpan<char>.Empty,
                    out object value);

                result.Should().BeTrue();
                value.Should().Be(true);
            }
        }
    }
}
