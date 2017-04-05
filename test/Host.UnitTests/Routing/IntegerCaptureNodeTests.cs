namespace Host.UnitTests.Routing
{
    using System;
    using Crest.Host;
    using Crest.Host.Routing;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class IntegerCaptureNodeTests
    {
        private const string ParameterName = "parameter";
        private IntegerCaptureNode node;

        [SetUp]
        public void SetUp()
        {
            this.node = new IntegerCaptureNode(ParameterName, typeof(int));
        }

        [TestFixture]
        public sealed class Constructor : IntegerCaptureNodeTests
        {
            [Test]
            public void ShouldThrowForInvalidIntegerTypes()
            {
                Action action = () => new IntegerCaptureNode("", typeof(Guid));
                action.ShouldThrow<ArgumentException>();

                action = () => new IntegerCaptureNode("", typeof(IntegerCaptureNodeTests));
                action.ShouldThrow<ArgumentException>();
            }
        }

        [TestFixture]
        public sealed new class Equals : IntegerCaptureNodeTests
        {
            [Test]
            public void ShouldReturnFalseForDifferentParameters()
            {
                var other = new IntegerCaptureNode(ParameterName + "New", typeof(int));
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnFalseForDifferentTypes()
            {
                var other = new IntegerCaptureNode(ParameterName, typeof(short));
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnFalseForNonIntegerCaptureNodes()
            {
                IMatchNode other = Substitute.For<IMatchNode>();
                this.node.Equals(other).Should().BeFalse();
            }

            [Test]
            public void ShouldReturnTrueForTheSameParameter()
            {
                var other = new IntegerCaptureNode(ParameterName, typeof(int));
                this.node.Equals(other).Should().BeTrue();
            }
        }

        [TestFixture]
        public sealed class Match : IntegerCaptureNodeTests
        {
            [TestCase("123", ExpectedResult = 123)]
            [TestCase("+123", ExpectedResult = 123)]
            [TestCase("-123", ExpectedResult = -123)]
            public int ShouldMatchValidIntegers(string integer)
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("/" + integer + "/", 1, integer.Length + 1));

                result.Success.Should().BeTrue();
                result.Name.Should().Be(ParameterName);
                return (int)result.Value;
            }

            [Test]
            public void ShouldNotMatchInvalidIntegers()
            {
                NodeMatchResult result = this.node.Match(
                    new StringSegment("/ABC/", 1, 4));

                result.Success.Should().BeFalse();
            }
        }

        [TestFixture]
        public sealed class Priority : IntegerCaptureNodeTests
        {
            [Test]
            public void ShouldReturnAPositiveValue()
            {
                this.node.Priority.Should().BePositive();
            }
        }
    }
}
