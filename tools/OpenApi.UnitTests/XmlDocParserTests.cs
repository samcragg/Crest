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
        public void GetClassDescriptionShouldReturnEmptyForUnknownTypes()
        {
            ClassDescription result = this.parser.GetClassDescription(typeof(XmlDocParserTests));

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Remarks, Is.Null);
            Assert.That(result.Summary, Is.Null);
        }

        [Test]
        public void GetClassDescriptionShouldReturnTheRemarks()
        {
            ClassDescription result = this.parser.GetClassDescription(typeof(ExampleClass));

            Assert.That(result.Remarks, Is.EqualTo("Remarks for the class."));
        }

        [Test]
        public void GetClassDescriptionShouldReturnTheSummmary()
        {
            ClassDescription result = this.parser.GetClassDescription(typeof(ExampleClass));

            Assert.That(result.Summary, Is.EqualTo("Summary for the class."));
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

        [Test]
        public void GetMethodDescriptionShouldReturnEmptyForUnknownTypes()
        {
            ClassDescription result = this.parser.GetClassDescription(typeof(XmlDocParserTests));

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Remarks, Is.Null);
            Assert.That(result.Summary, Is.Null);
        }

        [Test]
        public void GetMethodDescriptionShouldReturnTheParameters()
        {
            MethodInfo method = typeof(ExampleClass).GetMethod(nameof(ExampleClass.Method));

            MethodDescription result = this.parser.GetMethodDescription(method);

            Assert.That(result.Parameters, Has.Count.EqualTo(1));
            Assert.That(result.Parameters["parameter"], Is.EqualTo("Parameter description."));
        }

        [Test]
        public void GetMethodDescriptionShouldReturnTheRemarks()
        {
            MethodInfo method = typeof(ExampleClass).GetMethod(nameof(ExampleClass.Method));

            MethodDescription result = this.parser.GetMethodDescription(method);

            Assert.That(result.Remarks, Is.EqualTo("Remarks for the method."));
        }

        [Test]
        public void GetMethodDescriptionShouldReturnTheReturns()
        {
            MethodInfo method = typeof(ExampleClass).GetMethod(nameof(ExampleClass.Method));

            MethodDescription result = this.parser.GetMethodDescription(method);

            Assert.That(result.Returns, Is.EqualTo("Return description for the method."));
        }

        [Test]
        public void GetMethodDescriptionShouldReturnTheSummmary()
        {
            MethodInfo method = typeof(ExampleClass).GetMethod(nameof(ExampleClass.Method));

            MethodDescription result = this.parser.GetMethodDescription(method);

            Assert.That(result.Summary, Is.EqualTo("Summary for the method."));
        }

        [Test]
        public void GetMethodDescriptionShouldHandleParameterlessMethods()
        {
            MethodInfo method = typeof(ExampleClass).GetMethod(nameof(ExampleClass.MethodWithoutParameter));

            MethodDescription result = this.parser.GetMethodDescription(method);

            Assert.That(result.Summary, Is.EqualTo("Summary for the method."));
            Assert.That(result.Parameters, Is.Empty);
        }
    }
}
