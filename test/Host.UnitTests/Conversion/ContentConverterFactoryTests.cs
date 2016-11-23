namespace Host.UnitTests.Conversion
{
    using Crest.Host.Conversion;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ContentConverterFactoryTests
    {
        [Test]
        public void ShouldReturnTheConverterThatMatchesTheAcceptExactly()
        {
            var converter = Substitute.For<IContentConverter>();
            converter.Formats.Returns(new[] { "application/test" });
            var factory = new ContentConverterFactory(new[] { converter });

            IContentConverter result = factory.GetConverter("application/test");

            Assert.That(result, Is.SameAs(converter));
        }
    }
}
