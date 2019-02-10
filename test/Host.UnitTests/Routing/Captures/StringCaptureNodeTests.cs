namespace Host.UnitTests.Routing.Captures
{
    using System;
    using Crest.Host.Routing;
    using Crest.Host.Routing.Captures;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class StringCaptureNodeTests
    {
        private const string Parameter = "parameter";
        private readonly StringCaptureNode node = new StringCaptureNode(Parameter);

        public new sealed class Equals : StringCaptureNodeTests
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
                NodeMatchInfo result = this.node.Match("string".AsSpan());

                result.Success.Should().BeTrue();
            }

            [Fact]
            public void ShouldMatchUptoThePathSeparator()
            {
                NodeMatchInfo result = this.node.Match("one/two".AsSpan());

                result.Value.Should().Be("one");
            }

            [Fact]
            public void ShouldSaveTheCapturedParameter()
            {
                NodeMatchInfo result = this.node.Match("23".AsSpan());

                result.Parameter.Should().Be(Parameter);
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
                    "string".AsSpan(),
                    out object value);

                result.Should().BeTrue();
                value.Should().Be("string");
            }
        }
    }
}
