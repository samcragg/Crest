namespace Core.UnitTests
{
    using Crest.Core;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class PostAttributeTests
    {
        [TestFixture]
        public sealed class Route : PostAttributeTests
        {
            private const string ExampleRoute = "example/route";

            [Test]
            public void ShouldReturnTheRoutePassedInToTheConstructor()
            {
                var attribute = new PostAttribute(ExampleRoute);

                attribute.Route.Should().Be(ExampleRoute);
            }
        }

        [TestFixture]
        public sealed class Verb : PostAttributeTests
        {
            [Test]
            public void ShouldReturnPOST()
            {
                var attribute = new PostAttribute(string.Empty);

                attribute.Verb.Should().Be("POST");
            }
        }
    }
}
