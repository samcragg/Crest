namespace Host.UnitTests.Conversion
{
    using Crest.Host.Conversion;
    using FluentAssertions;
    using Xunit;

    public class HtmlTemplateProviderTests
    {
        public sealed class ContentLocation : HtmlTemplateProviderTests
        {
            [Fact]
            public void ShouldReturnTheLocationAfterTheBodyTag()
            {
                var provider = new HtmlTemplateProvider();

                string beforeLocation = provider.Template.Substring(0, provider.ContentLocation);

                beforeLocation.Should().EndWith("<body>");
            }
        }

        public sealed class HintText : HtmlTemplateProviderTests
        {
            [Fact]
            public void ShouldReturnANonEmptyValue()
            {
                var provider = new HtmlTemplateProvider();

                provider.HintText.Should().NotBeNullOrEmpty();
            }
        }

        public sealed class Template : HtmlTemplateProviderTests
        {
            [Fact]
            public void ShouldReturnANonEmptyValue()
            {
                var provider = new HtmlTemplateProvider();

                provider.Template.Should().NotBeNullOrEmpty();
            }
        }
    }
}
