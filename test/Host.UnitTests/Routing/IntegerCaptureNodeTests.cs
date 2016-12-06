namespace Host.UnitTests.Routing
{
    using System;
    using Crest.Host;
    using Crest.Host.Routing;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class IntegerCaptureNodeTests
    {
        private const string ParameterName = "parameter";
        private IntegerCaptureNode node;

        [SetUp]
        public void SetUp()
        {
            this.node = new IntegerCaptureNode(ParameterName, typeof(int));
        }

        [Test]
        public void ConstructorShouldThrowForInvalidIntegerTypes()
        {
            Assert.That(
                () => new IntegerCaptureNode("", typeof(Guid)),
                Throws.InstanceOf<ArgumentException>());

            Assert.That(
                () => new IntegerCaptureNode("", typeof(IntegerCaptureNodeTests)),
                Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void PriorityShouldReturnAPositiveValue()
        {
            Assert.That(this.node.Priority, Is.Positive);
        }

        [Test]
        public void EqualsShouldReturnFalseForNonIntegerCaptureNodes()
        {
            IMatchNode other = Substitute.For<IMatchNode>();
            Assert.That(this.node.Equals(other), Is.False);
        }

        [Test]
        public void EqualsShouldReturnFalseForDifferentParameters()
        {
            var other = new IntegerCaptureNode(ParameterName + "New", typeof(int));
            Assert.That(this.node.Equals(other), Is.False);
        }

        [Test]
        public void EqualsShouldReturnFalseForDifferentTypes()
        {
            var other = new IntegerCaptureNode(ParameterName, typeof(short));
            Assert.That(this.node.Equals(other), Is.False);
        }

        [Test]
        public void EqualsShouldReturnTrueForTheSameParameter()
        {
            var other = new IntegerCaptureNode(ParameterName, typeof(int));
            Assert.That(this.node.Equals(other), Is.True);
        }

        [TestCase("123", ExpectedResult = 123)]
        [TestCase("+123", ExpectedResult = 123)]
        [TestCase("-123", ExpectedResult = -123)]
        public int ShouldMatchValidIntegers(string integer)
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("/" + integer + "/", 1, integer.Length + 1));

            Assert.That(result.Success, Is.True);
            Assert.That(result.Name, Is.EqualTo(ParameterName));
            return (int)result.Value;
        }

        [Test]
        public void ShouldNotMatchInvalidIntegers()
        {
            NodeMatchResult result = this.node.Match(
                new StringSegment("/ABC/", 1, 4));

            Assert.That(result.Success, Is.False);
        }
    }
}
