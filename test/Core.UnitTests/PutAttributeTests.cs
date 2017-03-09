namespace Core.UnitTests
{
    using Crest.Core;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class PutAttributeTests
    {
        [TestFixture]
        public sealed class Route : PutAttributeTests
        {
            private const string ExampleRoute = "example/route";

            [Test]
            public void ShouldReturnTheRoutePassedInToTheConstructor()
            {
                var attribute = new PutAttribute(ExampleRoute);

                attribute.Route.Should().Be(ExampleRoute);
            }
        }

        [TestFixture]
        public sealed class Verb : PutAttributeTests
        {
            [Test]
            public void ShouldReturnPUT()
            {
                var attribute = new PutAttribute(string.Empty);

                attribute.Verb.Should().Be("PUT");
            }
        }
    }
}
