namespace Core.UnitTests
{
    using Crest.Core;
    using NUnit.Framework;

    [TestFixture]
    public sealed class PutAttributeTests
    {
        [Test]
        public void RouteShouldReturnTheRoutePassedInToTheConstructor()
        {
            const string Route = "example/route";

            var attribute = new PutAttribute(Route);

            Assert.That(attribute.Route, Is.EqualTo(Route));
        }

        [Test]
        public void VerbShouldReturnPUT()
        {
            var attribute = new PutAttribute(string.Empty);

            Assert.That(attribute.Verb, Is.EqualTo("PUT"));
        }
    }
}
