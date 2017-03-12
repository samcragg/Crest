namespace Host.UnitTests.Conversion
{
    using Crest.Host.Conversion;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class HtmlTemplateProviderTests
    {
        [TestFixture]
        public sealed class ContentLocation : HtmlTemplateProviderTests
        {
            [Test]
            public void ShouldReturnTheLocationAfterTheBodyTag()
            {
                var provider = new HtmlTemplateProvider();

                string beforeLocation = provider.Template.Substring(0, provider.ContentLocation);

                beforeLocation.Should().EndWith("<body>");
            }
        }

        [TestFixture]
        public sealed class HintText : HtmlTemplateProviderTests
        {
            [Test]
            public void ShouldReturnANonEmptyValue()
            {
                var provider = new HtmlTemplateProvider();

                provider.HintText.Should().NotBeNullOrEmpty();
            }
        }

        [TestFixture]
        public sealed class Template : HtmlTemplateProviderTests
        {
            [Test]
            public void ShouldReturnANonEmptyValue()
            {
                var provider = new HtmlTemplateProvider();

                provider.Template.Should().NotBeNullOrEmpty();
            }
        }
    }
}
