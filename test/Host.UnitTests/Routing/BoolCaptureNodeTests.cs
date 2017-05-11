namespace Host.UnitTests.Routing
{
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class BoolCaptureNodeTests
    {
        private const string ParameterName = "parameter";
        private readonly BoolCaptureNode node = new BoolCaptureNode(ParameterName);

        public sealed new class Equals : BoolCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnFalseForDifferentParameters()
            {
                var other = new BoolCaptureNode(ParameterName + "New");
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
                var other = new BoolCaptureNode(ParameterName);
                this.node.Equals(other).Should().BeTrue();
            }
        }

        public sealed class Match : BoolCaptureNodeTests
        {
            [Fact]
            public void ShouldMatchFalse()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("/False/", 1, 6));

                result.Success.Should().BeTrue();
            }

            [Fact]
            public void ShouldMatchOneAsTrue()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("_1_", 1, 2));

                result.Success.Should().BeTrue();
                result.Value.Should().Be(true);
            }

            [Fact]
            public void ShouldMatchTrue()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("/True/", 1, 5));

                result.Success.Should().BeTrue();
            }

            [Fact]
            public void ShouldMatchZeroAsFalse()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("_0_", 1, 2));

                result.Success.Should().BeTrue();
                result.Value.Should().Be(false);
            }

            [Fact]
            public void ShouldNotMatchPartialWords()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("_true_", 1, 4));

                result.Success.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTheCapturedParameter()
            {
                NodeMatchResult result = this.node.Match(new StringSegment("true"));

                result.Name.Should().Be(ParameterName);
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
                    new StringSegment("_false_", 1, 6),
                    out object value);

                result.Should().BeTrue();
                value.Should().Be(false);
            }

            [Fact]
            public void ShouldMatchTrue()
            {
                bool result = this.node.TryConvertValue(
                    new StringSegment("_true_", 1, 5),
                    out object value);

                result.Should().BeTrue();
                value.Should().Be(true);
            }

            [Fact]
            public void ShouldReturnFalseForInvalidValues()
            {
                bool result = this.node.TryConvertValue(
                    new StringSegment("invalid"),
                    out object value);

                result.Should().BeFalse();
                value.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTrueForEmptyValues()
            {
                bool result = this.node.TryConvertValue(
                    new StringSegment(string.Empty),
                    out object value);

                result.Should().BeTrue();
                value.Should().Be(true);
            }
        }
    }
}
