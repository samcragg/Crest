namespace Host.UnitTests.Routing
{
    using System;
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class GenericCaptureNodeTests
    {
        private const string ParameterName = "parameter";
        private readonly GenericCaptureNode node = new GenericCaptureNode(ParameterName, typeof(int));

        public sealed new class Equals : GenericCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnFalseForDifferentParameters()
            {
                var other = new GenericCaptureNode(ParameterName + "New", typeof(int));
                this.node.Equals(other).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForDifferentTypes()
            {
                var other = new GenericCaptureNode(ParameterName, typeof(Guid));
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
                var other = new GenericCaptureNode(ParameterName, typeof(int));
                this.node.Equals(other).Should().BeTrue();
            }
        }

        public sealed class Match : GenericCaptureNodeTests
        {
            [Fact]
            public void ShouldReturnNoneIfTheConversionErrored()
            {
                var result = this.node.Match(new StringSegment("Not an integer", 0, 14));

                result.Success.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnSuccessIfTheConversionSucceeded()
            {
                var result = this.node.Match(new StringSegment("1", 0, 1));

                result.Success.Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnTheConvertedParameter()
            {
                var result = this.node.Match(new StringSegment("123", 0, 3));

                result.Name.Should().Be(ParameterName);
                result.Value.Should().Be(123);
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
    }
}
