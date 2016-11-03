namespace Core.UnitTests
{
    using Crest.Core;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DeleteAttributeTests
    {
        [Test]
        public void RouteShouldReturnTheRoutePassedInToTheConstructor()
        {
            const string Route = "example/route";

            var attribute = new DeleteAttribute(Route);

            Assert.That(attribute.Route, Is.EqualTo(Route));
        }

        [Test]
        public void VerbShouldReturnDELETE()
        {
            var attribute = new DeleteAttribute(string.Empty);

            Assert.That(attribute.Verb, Is.EqualTo("DELETE"));
        }
    }
}
