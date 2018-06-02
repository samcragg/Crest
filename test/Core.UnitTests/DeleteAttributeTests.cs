namespace Core.UnitTests
{
    using Crest.Core;
    using FluentAssertions;
    using Xunit;

    public class DeleteAttributeTests
    {
        public sealed class CanReadBody : DeleteAttributeTests
        {
            [Fact]
            public void ShouldReturnFalse()
            {
                var attribute = new DeleteAttribute(string.Empty);

                attribute.CanReadBody.Should().BeFalse();
            }
        }

        public sealed class Route : DeleteAttributeTests
        {
            private const string ExampleRoute = "example/route";

            [Fact]
            public void ShouldReturnTheRoutePassedInToTheConstructor()
            {
                var attribute = new DeleteAttribute(ExampleRoute);

                attribute.Route.Should().Be(ExampleRoute);
            }
        }

        public sealed class Verb : DeleteAttributeTests
        {
            [Fact]
            public void ShouldReturnDELETE()
            {
                var attribute = new DeleteAttribute(string.Empty);

                attribute.Verb.Should().Be("DELETE");
            }
        }
    }
}
