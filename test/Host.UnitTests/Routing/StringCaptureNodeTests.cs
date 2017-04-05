namespace Host.UnitTests.Routing
{
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class StringCaptureNodeTests
    {
        private const string ParameterName = "parameter";
        private readonly StringCaptureNode node = new StringCaptureNode(ParameterName);

        public sealed new class Equals : StringCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnFalseForDifferentParameters()
            {
                var other = new StringCaptureNode(ParameterName + "New");
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
                var other = new StringCaptureNode(ParameterName);
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

                result.Name.Should().Be(ParameterName);
                result.Value.Should().Be("23");
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
    }
}
