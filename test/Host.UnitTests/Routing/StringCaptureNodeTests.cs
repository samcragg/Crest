namespace Host.UnitTests.Routing
{
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class StringCaptureNodeTests
    {
        private const string Parameter = "parameter";
        private readonly StringCaptureNode node = new StringCaptureNode(Parameter);

        public sealed new class Equals : StringCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnFalseForDifferentParameters()
            {
                var other = new StringCaptureNode(Parameter + "New");
                this.node.Equals(other).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForNonStringCaptureNodes()
            {
                IMatchNode other = Substitute.For<IMatchNode>();
                this.node.Equals(other).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueForTheSameParameter()
            {
                var other = new StringCaptureNode(Parameter);
                this.node.Equals(other).Should().BeTrue();
            }
        }

        public sealed class Match : StringCaptureNodeTests
        {
            [Fact]
            public void ShouldMatchAnyString()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("/string/", 1, 7));

                result.Success.Should().BeTrue();
            }

            [Fact]
            public void ShouldSaveTheCapturedParameter()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("01234", 2, 4));

                result.Name.Should().Be(Parameter);
                result.Value.Should().Be("23");
            }
        }

        public sealed class ParameterName : StringCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnTheValuePassedInTheConstructor()
            {
                this.node.ParameterName.Should().Be(Parameter);
            }
        }

        public sealed class Priority : StringCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnAPositiveValue()
            {
                this.node.Priority.Should().BePositive();
            }
        }

        public sealed class TryConvertValue : StringCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnThePassedInSegment()
            {
                bool result = this.node.TryConvertValue(
                    new StringSegment("/string/", 1, 7),
                    out object value);

                result.Should().BeTrue();
                value.Should().Be("string");
            }
        }
    }
}
