namespace Core.UnitTests
{
    using Crest.Core;
    using FluentAssertions;
    using Xunit;

    public class PostAttributeTests
    {
        public sealed class CanReadBody : PostAttributeTests
        {
            [Fact]
            public void ShouldReturnTrue()
            {
                var attribute = new PostAttribute(string.Empty);

                attribute.CanReadBody.Should().BeTrue();
            }
        }

        public sealed class Route : PostAttributeTests
        {
            private const string ExampleRoute = "example/route";

            [Fact]
            public void ShouldReturnTheRoutePassedInToTheConstructor()
            {
                var attribute = new PostAttribute(ExampleRoute);

                attribute.Route.Should().Be(ExampleRoute);
            }
        }

        public sealed class Verb : PostAttributeTests
        {
            [Fact]
            public void ShouldReturnPOST()
            {
                var attribute = new PostAttribute(string.Empty);

                attribute.Verb.Should().Be("POST");
            }
        }
    }
}
