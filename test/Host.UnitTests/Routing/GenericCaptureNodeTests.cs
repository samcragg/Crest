namespace Host.UnitTests.Routing
{
    using System;
    using Crest.Host;
    using Crest.Host.Routing;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class GenericCaptureNodeTests
    {
        private const string ParameterName = "parameter";
        private GenericCaptureNode node;

        [SetUp]
        public void SetUp()
        {
            this.node = new GenericCaptureNode(ParameterName, typeof(int));
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
            var other = new GenericCaptureNode(ParameterName + "New", typeof(int));
            Assert.That(this.node.Equals(other), Is.False);
        }

        [Test]
        public void EqualsShouldReturnFalseForDifferentTypes()
        {
            var other = new GenericCaptureNode(ParameterName, typeof(Guid));
            Assert.That(this.node.Equals(other), Is.False);
        }

        [Test]
        public void EqualsShouldReturnTrueForTheSameParameter()
        {
            var other = new GenericCaptureNode(ParameterName, typeof(int));
            Assert.That(this.node.Equals(other), Is.True);
        }

        [Test]
        public void MatchShouldReturnSuccessIfTheConversionSucceeded()
        {
            var result = this.node.Match(new StringSegment("1", 0, 1));

            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void MatchShouldReturnTheConvertedParameter()
        {
            var result = this.node.Match(new StringSegment("123", 0, 3));

            Assert.That(result.Name, Is.EqualTo(ParameterName));
            Assert.That(result.Value, Is.EqualTo(123));
        }

        [Test]
        public void MatchShouldReturnNoneIfTheConversionErrored()
        {
            var result = this.node.Match(new StringSegment("Not an integer", 0, 14));

            Assert.That(result.Success, Is.False);
        }
    }
}
