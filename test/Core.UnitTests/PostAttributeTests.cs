namespace Core.UnitTests
{
    using Crest.Core;
    using NUnit.Framework;

    [TestFixture]
    public sealed class PostAttributeTests
    {
        [Test]
        public void RouteShouldReturnTheRoutePassedInToTheConstructor()
        {
            const string Route = "example/route";

            var attribute = new PostAttribute(Route);

            Assert.That(attribute.Route, Is.EqualTo(Route));
        }

        [Test]
        public void VerbShouldReturnPOST()
        {
            var attribute = new PostAttribute(string.Empty);

            Assert.That(attribute.Verb, Is.EqualTo("POST"));
        }
    }
}
