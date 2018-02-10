namespace Host.UnitTests.Conversion
{
    using System;
    using Crest.Abstractions;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class ContentConverterFactoryTests
    {
        public sealed class GetConverter : ContentConverterFactoryTests
        {
            [Fact]
            public void ShouldDefaultToJsonIfAcceptIsEmpty()
            {
                IContentConverter json = CreateConverter("application/json", 100);
                IContentConverter text = CreateConverter("text/plain", 100);
                var factory = new ContentConverterFactory(new[] { json, text });

                IContentConverter result = factory.GetConverter("");

                result.Should().BeSameAs(json);
            }

            [Fact]
            public void ShouldMatchTheAcceptWithTheHighestQualityAvailable()
            {
                IContentConverter application = CreateConverter("application/test", 100);
                IContentConverter text = CreateConverter("text/plain", 100);
                var factory = new ContentConverterFactory(new[] { application, text });

                IContentConverter result = factory.GetConverter("application/json, application/test;q=0.5, text/*;q=0.8");

                result.Should().BeSameAs(text);
            }

            [Fact]
            public void ShouldReturnNullIfNoMatches()
            {
                var factory = new ContentConverterFactory(new IContentConverter[0]);

                IContentConverter result = factory.GetConverter("");

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheConverterThatMatchesTheAcceptExactly()
            {
                IContentConverter converter = CreateConverter("application/test", 100);
                var factory = new ContentConverterFactory(new[] { converter });

                IContentConverter result = factory.GetConverter("application/test");

                result.Should().BeSameAs(converter);
            }

            [Fact]
            public void ShouldReturnTheConverterWithTheBestQuality()
            {
                IContentConverter low = CreateConverter("application/test;q=0.5", 100);
                IContentConverter high = CreateConverter("application/test;q=0.8", 20);
                var factory = new ContentConverterFactory(new[] { low, high });

                IContentConverter result = factory.GetConverter("application/test");

                result.Should().BeSameAs(high);
            }

            [Fact]
            public void ShouldReturnTheConverterWithTheHighestPriority()
            {
                IContentConverter low = CreateConverter("application/test", 5);
                IContentConverter high = CreateConverter("application/test", 10);
                var factory = new ContentConverterFactory(new[] { low, high });

                IContentConverter result = factory.GetConverter("application/test");

                result.Should().BeSameAs(high);
            }

            private static IContentConverter CreateConverter(string mime, int priority)
            {
                IContentConverter converter = Substitute.For<IContentConverter>();
                converter.Formats.Returns(new[] { mime });
                converter.Priority.Returns(priority);
                return converter;
            }
        }

        public sealed class PrimeConverters : ContentConverterFactoryTests
        {
            private readonly IContentConverter converter = Substitute.For<IContentConverter>();
            private readonly ContentConverterFactory factory;

            public PrimeConverters()
            {
                // The converters are stored against their formats so we need
                // to return something for it to be stored in the factory
                this.converter.Formats.Returns(new[] { "mime/type" });
                this.factory = new ContentConverterFactory(new[] { this.converter });
            }

            [Fact]
            public void ShouldCheckForNullArguments()
            {
                Action action = () => this.factory.PrimeConverters(null);

                action.Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void ShouldPassTheTypeToTheConverters()
            {
                this.factory.PrimeConverters(typeof(int));

                this.converter.Received().Prime(typeof(int));
            }
        }
    }
}
