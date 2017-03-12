namespace Host.UnitTests.Conversion
{
    using Crest.Host.Conversion;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class ContentConverterFactoryTests
    {
        [TestFixture]
        public sealed class GetConverter : ContentConverterFactoryTests
        {
            [Test]
            public void ShouldDefaultToJsonIfAcceptIsEmpty()
            {
                var json = CreateConverter("application/json", 100);
                var text = CreateConverter("text/plain", 100);
                var factory = new ContentConverterFactory(new[] { json, text });

                IContentConverter result = factory.GetConverter("");

                result.Should().BeSameAs(json);
            }

            [Test]
            public void ShouldMatchTheAcceptWithTheHighestQualityAvailable()
            {
                var application = CreateConverter("application/test", 100);
                var text = CreateConverter("text/plain", 100);
                var factory = new ContentConverterFactory(new[] { application, text });

                IContentConverter result = factory.GetConverter("application/json, application/test;q=0.5, text/*;q=0.8");

                result.Should().BeSameAs(text);
            }

            [Test]
            public void ShouldReturnNullIfNoMatches()
            {
                var factory = new ContentConverterFactory(new IContentConverter[0]);

                IContentConverter result = factory.GetConverter("");

                result.Should().BeNull();
            }

            [Test]
            public void ShouldReturnTheConverterThatMatchesTheAcceptExactly()
            {
                var converter = CreateConverter("application/test", 100);
                var factory = new ContentConverterFactory(new[] { converter });

                IContentConverter result = factory.GetConverter("application/test");

                result.Should().BeSameAs(converter);
            }

            [Test]
            public void ShouldReturnTheConverterWithTheBestQuality()
            {
                var low = CreateConverter("application/test;q=0.5", 100);
                var high = CreateConverter("application/test;q=0.8", 20);
                var factory = new ContentConverterFactory(new[] { low, high });

                IContentConverter result = factory.GetConverter("application/test");

                result.Should().BeSameAs(high);
            }

            [Test]
            public void ShouldReturnTheConverterWithTheHighestPriority()
            {
                var low = CreateConverter("application/test", 5);
                var high = CreateConverter("application/test", 10);
                var factory = new ContentConverterFactory(new[] { low, high });

                IContentConverter result = factory.GetConverter("application/test");

                result.Should().BeSameAs(high);
            }

            private static IContentConverter CreateConverter(string mime, int priority)
            {
                var converter = Substitute.For<IContentConverter>();
                converter.Formats.Returns(new[] { mime });
                converter.Priority.Returns(priority);
                return converter;
            }
        }
    }
}
