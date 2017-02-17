namespace Host.UnitTests.Conversion
{
    using Crest.Host.Conversion;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HtmlTemplateProviderTests
    {
        [Test]
        public void ContentLocationShouldReturnTheLocationAfterTheBodyTag()
        {
            var provider = new HtmlTemplateProvider();

            string beforeLocation = provider.Template.Substring(0, provider.ContentLocation);

            Assert.That(beforeLocation, Does.EndWith("<body>"));
        }

        [Test]
        public void HintTextShouldReturnANonEmptyValue()
        {
            var provider = new HtmlTemplateProvider();

            Assert.That(provider.HintText, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void TemplateShouldReturnANonEmptyValue()
        {
            var provider = new HtmlTemplateProvider();

            Assert.That(provider.Template, Is.Not.Null.Or.Empty);
        }
    }
}
