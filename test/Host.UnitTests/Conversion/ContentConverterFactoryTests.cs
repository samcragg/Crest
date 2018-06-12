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
        private static IContentConverter CreateConverter(string mime, int priority)
        {
            IContentConverter converter = Substitute.For<IContentConverter>();
            converter.CanRead.Returns(true);
            converter.CanWrite.Returns(true);
            converter.Formats.Returns(new[] { mime });
            converter.Priority.Returns(priority);
            return converter;
        }

        public sealed class GetConverterForAccept : ContentConverterFactoryTests
        {
            [Fact]
            public void ShouldDefaultToJsonIfAcceptIsEmpty()
            {
                IContentConverter json = CreateConverter("application/json", 100);
                IContentConverter text = CreateConverter("text/plain", 100);
                var factory = new ContentConverterFactory(new[] { json, text });

                IContentConverter result = factory.GetConverterForAccept("");

                result.Should().BeSameAs(json);
            }

            [Fact]
            public void ShouldIgnoreConvertersThatCannotWrite()
            {
                IContentConverter low = CreateConverter("application/test", 5);
                IContentConverter high = CreateConverter("application/test", 10);
                high.CanWrite.Returns(false);
                var factory = new ContentConverterFactory(new[] { low, high });

                IContentConverter result = factory.GetConverterForAccept("application/test");

                // Even though high has a higher priority, it shouldn't be
                // returned as it cannot write values
                result.Should().BeSameAs(low);
            }

            [Fact]
            public void ShouldMatchTheAcceptWithTheHighestQualityAvailable()
            {
                IContentConverter application = CreateConverter("application/test", 100);
                IContentConverter text = CreateConverter("text/plain", 100);
                var factory = new ContentConverterFactory(new[] { application, text });

                IContentConverter result = factory.GetConverterForAccept("application/json, application/test;q=0.5, text/*;q=0.8");

                result.Should().BeSameAs(text);
            }

            [Fact]
            public void ShouldReturnNullIfNoMatches()
            {
                var factory = new ContentConverterFactory(new IContentConverter[0]);

                IContentConverter result = factory.GetConverterForAccept("");

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheConverterThatMatchesTheAcceptExactly()
            {
                IContentConverter converter = CreateConverter("application/test", 100);
                var factory = new ContentConverterFactory(new[] { converter });

                IContentConverter result = factory.GetConverterForAccept("application/test");

                result.Should().BeSameAs(converter);
            }

            [Fact]
            public void ShouldReturnTheConverterWithTheBestQuality()
            {
                IContentConverter low = CreateConverter("application/test;q=0.5", 100);
                IContentConverter high = CreateConverter("application/test;q=0.8", 20);
                var factory = new ContentConverterFactory(new[] { low, high });

                IContentConverter result = factory.GetConverterForAccept("application/test");

                result.Should().BeSameAs(high);
            }

            [Fact]
            public void ShouldReturnTheConverterWithTheHighestPriority()
            {
                IContentConverter low = CreateConverter("application/test", 5);
                IContentConverter high = CreateConverter("application/test", 10);
                var factory = new ContentConverterFactory(new[] { low, high });

                IContentConverter result = factory.GetConverterForAccept("application/test");

                result.Should().BeSameAs(high);
            }
        }

        public sealed class GetConverterFromContentType : ContentConverterFactoryTests
        {
            [Fact]
            public void ShouldIgnoreConvertersThatCannotRead()
            {
                IContentConverter low = CreateConverter("application/test", 5);
                IContentConverter high = CreateConverter("application/test", 10);
                high.CanRead.Returns(false);
                var factory = new ContentConverterFactory(new[] { low, high });

                IContentConverter result = factory.GetConverterFromContentType("application/test");

                // Even though high has a higher priority, it shouldn't be
                // returned as it cannot write values
                result.Should().BeSameAs(low);
            }

            [Fact]
            public void ShouldReturnNullForEmptyContentTypes()
            {
                var factory = new ContentConverterFactory(new IContentConverter[0]);

                IContentConverter result = factory.GetConverterFromContentType("");

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnNullIfNoMatches()
            {
                var factory = new ContentConverterFactory(new IContentConverter[0]);

                IContentConverter result = factory.GetConverterFromContentType("text/plain");

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheConverterThatMatchesTheContentType()
            {
                IContentConverter converter = CreateConverter("application/*", 100);
                var factory = new ContentConverterFactory(new[] { converter });

                IContentConverter result = factory.GetConverterFromContentType("application/test");

                result.Should().BeSameAs(converter);
            }

            [Fact]
            public void ShouldReturnTheConverterWithTheHighestPriority()
            {
                IContentConverter low = CreateConverter("application/test", 5);
                IContentConverter high = CreateConverter("application/test", 10);
                var factory = new ContentConverterFactory(new[] { low, high });

                IContentConverter result = factory.GetConverterFromContentType("application/test");

                result.Should().BeSameAs(high);
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
