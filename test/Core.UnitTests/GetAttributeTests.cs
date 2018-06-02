namespace Core.UnitTests
{
    using Crest.Core;
    using FluentAssertions;
    using Xunit;

    public class GetAttributeTests
    {
        public sealed class CanReadBody : GetAttributeTests
        {
            [Fact]
            public void ShouldReturnFalse()
            {
                var attribute = new GetAttribute(string.Empty);

                attribute.CanReadBody.Should().BeFalse();
            }
        }

        public sealed class Route : GetAttributeTests
        {
            private const string ExampleRoute = "example/route";

            [Fact]
            public void ShouldReturnTheRoutePassedInToTheConstructor()
            {
                var attribute = new GetAttribute(ExampleRoute);

                attribute.Route.Should().Be(ExampleRoute);
            }
        }

        public sealed class Verb : GetAttributeTests
        {
            [Fact]
            public void ShouldReturnGET()
            {
                var attribute = new GetAttribute(string.Empty);

                attribute.Verb.Should().Be("GET");
            }
        }
    }
}
