namespace Core.UnitTests
{
    using Crest.Core;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class GetAttributeTests
    {
        [TestFixture]
        public sealed class Route : GetAttributeTests
        {
            private const string ExampleRoute = "example/route";

            [Test]
            public void ShouldReturnTheRoutePassedInToTheConstructor()
            {
                var attribute = new GetAttribute(ExampleRoute);

                attribute.Route.Should().Be(ExampleRoute);
            }
        }

        [TestFixture]
        public sealed class Verb : GetAttributeTests
        {
            [Test]
            public void ShouldReturnGET()
            {
                var attribute = new GetAttribute(string.Empty);

                attribute.Verb.Should().Be("GET");
            }
        }
    }
}
