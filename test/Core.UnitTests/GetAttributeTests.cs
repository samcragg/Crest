namespace Core.UnitTests
{
    using Crest.Core;
    using NUnit.Framework;

    [TestFixture]
    public sealed class GetAttributeTests
    {
        [Test]
        public void RouteShouldReturnTheRoutePassedInToTheConstructor()
        {
            const string Route = "example/route";

            var attribute = new GetAttribute(Route);

            Assert.That(attribute.Route, Is.EqualTo(Route));
        }

        [Test]
        public void VerbShouldReturnGET()
        {
            var attribute = new GetAttribute(string.Empty);

            Assert.That(attribute.Verb, Is.EqualTo("GET"));
        }
    }
}
