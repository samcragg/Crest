namespace OpenApi.UnitTests
{
    using System.Reflection;
    using Crest.OpenApi;
    using NUnit.Framework;

    [TestFixture]
    public sealed class XmlDocParserTests
    {
        private XmlDocParser parser;

        public string Property { get; set; }

        [SetUp]
        public void SetUp()
        {
            Assembly assembly = typeof(XmlDocParserTests).GetTypeInfo().Assembly;
            this.parser = new XmlDocParser(assembly.GetManifestResourceStream("OpenApi.UnitTests.ExampleClass.xml"));
        }

        [Test]
        public void GetDescriptionShouldReturnNullForUnknownProperties()
        {
            PropertyInfo property = typeof(XmlDocParserTests).GetProperty(nameof(XmlDocParserTests.Property));

            string description = this.parser.GetDescription(property);

            Assert.That(description, Is.Null);
        }

        [Test]
        public void GetDescriptionShouldGetThePropertySummary()
        {
            PropertyInfo property = typeof(ExampleClass).GetProperty(nameof(ExampleClass.Property));

            string description = this.parser.GetDescription(property);

            Assert.That(description, Is.EqualTo("The summary for the property."));
        }

        [Test]
        public void GetDescriptionShouldRemoveGetsFromTheSummary()
        {
            PropertyInfo property = typeof(ExampleClass).GetProperty(nameof(ExampleClass.GetProperty));

            string description = this.parser.GetDescription(property);

            Assert.That(description, Is.EqualTo("The summary for the property."));
        }

        [Test]
        public void GetDescriptionShouldRemoveGetsOrSetsFromTheSummary()
        {
            PropertyInfo property = typeof(ExampleClass).GetProperty(nameof(ExampleClass.GetSetProperty));

            string description = this.parser.GetDescription(property);

            Assert.That(description, Is.EqualTo("The summary for the property."));
        }
    }
}
