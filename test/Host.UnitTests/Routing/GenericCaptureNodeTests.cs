namespace Host.UnitTests.Routing
{
    using System;
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class GenericCaptureNodeTests
    {
        private const string ParameterName = "parameter";
        private GenericCaptureNode node;

        [SetUp]
        public void SetUp()
        {
            this.node = new GenericCaptureNode(ParameterName, typeof(int));
        }

        [TestFixture]
        public sealed new class Equals : GenericCaptureNodeTests
        {
            [Test]
            public void ShouldReturnFalseForDifferentParameters()
            {
                var other = new GenericCaptureNode(ParameterName + "New", typeof(int));
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnFalseForDifferentTypes()
            {
                var other = new GenericCaptureNode(ParameterName, typeof(Guid));
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnFalseForNonGenericCaptureNodes()
            {
                IMatchNode other = Substitute.For<IMatchNode>();
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnTrueForTheSameParameter()
            {
                var other = new GenericCaptureNode(ParameterName, typeof(int));
                this.node.Equals(other).Should().BeTrue();
            }
        }

        [TestFixture]
        public sealed class Match : GenericCaptureNodeTests
        {
            [Test]
            public void ShouldReturnNoneIfTheConversionErrored()
            {
                var result = this.node.Match(new StringSegment("Not an integer", 0, 14));

                result.Success.Should().BeFalse();
            }

            [Test]
            public void ShouldReturnSuccessIfTheConversionSucceeded()
            {
                var result = this.node.Match(new StringSegment("1", 0, 1));

                result.Success.Should().BeTrue();
            }

            [Test]
            public void ShouldReturnTheConvertedParameter()
            {
                var result = this.node.Match(new StringSegment("123", 0, 3));

                result.Name.Should().Be(ParameterName);
                result.Value.Should().Be(123);
            }
        }

        [TestFixture]
        public sealed class Priority : GenericCaptureNodeTests
        {
            [Test]
            public void ShouldReturnAPositiveValue()
            {
                this.node.Priority.Should().BePositive();
            }
        }
    }
}
