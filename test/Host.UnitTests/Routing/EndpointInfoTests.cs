namespace Host.UnitTests.Routing
{
    using Crest.Host.Routing;
    using FluentAssertions;
    using Xunit;

    public class EndpointInfoTests
    {
        public sealed new class Equals : EndpointInfoTests
        {
            [Fact]
            public void ShouldReturnFalseForDifferentTypes()
            {
                var endpoint = new EndpointInfo<int>("GET", 0, 0, 0);

                endpoint.Equals("GET").Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseForNullValues()
            {
                var endpoint = new EndpointInfo<int>("GET", 0, 0, 0);

                endpoint.Equals(null).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseIfTheVerbIsDifferent()
            {
                var first = new EndpointInfo<int>("GET", 0, 0, 0);
                var second = new EndpointInfo<int>("PUT", 0, 0, 0);

                first.Equals(second).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnFalseIfTheVersionDoNotOverlap()
            {
                var first = new EndpointInfo<int>("GET", 0, 1, 1);
                var second = new EndpointInfo<int>("GET", 0, 2, 2);

                first.Equals(second).Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfTheVerbAndVersionsAreTheSame()
            {
                var first = new EndpointInfo<int>("GET", 0, 1, 1);
                var second = new EndpointInfo<int>("GET", 0, 1, 1);

                first.Equals(second).Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnTrueIfTheVersionsOverlap()
            {
                var first = new EndpointInfo<int>("GET", 0, 1, 3);
                var second = new EndpointInfo<int>("GET", 0, 2, 4);

                first.Equals(second).Should().BeTrue();
                second.Equals(first).Should().BeTrue();
            }
        }

        public sealed new class GetHashCode : EndpointInfoTests
        {
            [Fact]
            public void ShouldReturnTheSameValueForEqualInstances()
            {
                var first = new EndpointInfo<int>("GET", 0, 1, 1);
                var second = new EndpointInfo<int>("GET", 0, 1, 1);

                first.GetHashCode().Should().Be(second.GetHashCode());
            }
        }
    }
}
