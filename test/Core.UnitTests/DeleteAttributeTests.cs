namespace Core.UnitTests
{
    using Crest.Core;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class DeleteAttributeTests
    {
        [TestFixture]
        public sealed class Route : DeleteAttributeTests
        {
            private const string ExampleRoute = "example/route";

            [Test]
            public void ShouldReturnTheRoutePassedInToTheConstructor()
            {
                var attribute = new DeleteAttribute(ExampleRoute);

                attribute.Route.Should().Be(ExampleRoute);
            }
        }

        [TestFixture]
        public sealed class Verb : DeleteAttributeTests
        {
            [Test]
            public void ShouldReturnDELETE()
            {
                var attribute = new DeleteAttribute(string.Empty);

                attribute.Verb.Should().Be("DELETE");
            }
        }
    }
}
