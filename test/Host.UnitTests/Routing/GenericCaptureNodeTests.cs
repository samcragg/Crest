namespace Host.UnitTests.Routing
{
    using System;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class GenericCaptureNodeTests
    {
        private const string Parameter = "parameter";
        private readonly GenericCaptureNode node = new GenericCaptureNode(Parameter, typeof(int));

        public new sealed class Equals : GenericCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnFalseForDifferentParameters()
            {
                var other = new GenericCaptureNode(Parameter + "New", typeof(int));
                this.node.Equals(other).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForDifferentTypes()
            {
                var other = new GenericCaptureNode(Parameter, typeof(Guid));
                this.node.Equals(other).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForNonGenericCaptureNodes()
            {
                IMatchNode other = Substitute.For<IMatchNode>();
                this.node.Equals(other).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueForTheSameParameter()
            {
                var other = new GenericCaptureNode(Parameter, typeof(int));
                this.node.Equals(other).Should().BeTrue();
            }
        }

        public sealed class Match : GenericCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnNoneIfTheConversionErrored()
            {
                NodeMatchResult result = this.node.Match("Not an integer".AsSpan());

                result.Success.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnSuccessIfTheConversionSucceeded()
            {
                NodeMatchResult result = this.node.Match("1".AsSpan());

                result.Success.Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnTheConvertedParameter()
            {
                NodeMatchResult result = this.node.Match("123".AsSpan());

                result.Name.Should().Be(Parameter);
                result.Value.Should().Be(123);
            }
        }

        public sealed class ParameterName : GenericCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnTheValuePassedInTheConstructor()
            {
                this.node.ParameterName.Should().Be(Parameter);
            }
        }

        public sealed class Priority : GenericCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnAPositiveValue()
            {
                this.node.Priority.Should().BePositive();
            }
        }

        public sealed class TryConvertValue : GenericCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnFalsefTheConversionFailed()
            {
                bool result = this.node.TryConvertValue("Not an integer".AsSpan(), out object value);

                result.Should().BeFalse();
                value.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTrueIfTheConversionSucceeded()
            {
                bool result = this.node.TryConvertValue("1".AsSpan(), out object value);

                result.Should().BeTrue();
                value.Should().Be(1);
            }
        }
    }
}
