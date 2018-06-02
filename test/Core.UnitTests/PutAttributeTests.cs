namespace Core.UnitTests
{
    using Crest.Core;
    using FluentAssertions;
    using Xunit;

    public class PutAttributeTests
    {
        public sealed class CanReadBody : PutAttributeTests
        {
            [Fact]
            public void ShouldReturnTrue()
            {
                var attribute = new PutAttribute(string.Empty);

                attribute.CanReadBody.Should().BeTrue();
            }
        }

        public sealed class Route : PutAttributeTests
        {
            private const string ExampleRoute = "example/route";

            [Fact]
            public void ShouldReturnTheRoutePassedInToTheConstructor()
            {
                var attribute = new PutAttribute(ExampleRoute);

                attribute.Route.Should().Be(ExampleRoute);
            }
        }

        public sealed class Verb : PutAttributeTests
        {
            [Fact]
            public void ShouldReturnPUT()
            {
                var attribute = new PutAttribute(string.Empty);

                attribute.Verb.Should().Be("PUT");
            }
        }
    }
}
