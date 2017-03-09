namespace Core.UnitTests
{
    using Crest.Core;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class VersionAttributeTests
    {
        [TestFixture]
        public sealed class From : VersionAttributeTests
        {
            [Test]
            public void ShouldReturnTheValueFromTheConstructor()
            {
                var instance = new VersionAttribute(123);

                instance.From.Should().Be(123);
            }
        }

        [TestFixture]
        public sealed class To : VersionAttributeTests
        {
            [Test]
            public void ShouldDefaultToIntMaxValue()
            {
                var instance = new VersionAttribute(0);

                instance.To.Should().Be(int.MaxValue);
            }

            [Test]
            public void ShouldReturnTheValueFromTheConstructor()
            {
                var instance = new VersionAttribute(0, 123);

                instance.To.Should().Be(123);
            }
        }
    }
}
