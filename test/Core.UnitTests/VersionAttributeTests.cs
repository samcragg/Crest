namespace Core.UnitTests
{
    using Crest.Core;
    using FluentAssertions;
    using Xunit;

    public class VersionAttributeTests
    {
        public sealed class From : VersionAttributeTests
        {
            [Fact]
            public void ShouldReturnTheValueFromTheConstructor()
            {
                var instance = new VersionAttribute(123);

                instance.From.Should().Be(123);
            }
        }

        public sealed class To : VersionAttributeTests
        {
            [Fact]
            public void ShouldDefaultToIntMaxValue()
            {
                var instance = new VersionAttribute(0);

                instance.To.Should().Be(int.MaxValue);
            }

            [Fact]
            public void ShouldReturnTheValueFromTheConstructor()
            {
                var instance = new VersionAttribute(0, 123);

                instance.To.Should().Be(123);
            }
        }
    }
}
