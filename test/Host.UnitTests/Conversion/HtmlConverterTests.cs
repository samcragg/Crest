namespace Host.UnitTests.Conversion
{
    using System.IO;
    using System.Text;
    using Crest.Host;
    using Crest.Host.Conversion;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class HtmlConverterTests
    {
        private HtmlConverter converter;
        private IHtmlTemplateProvider templateProvider;

        [SetUp]
        public void SetUp()
        {
            this.templateProvider = Substitute.For<IHtmlTemplateProvider>();
            this.converter = new HtmlConverter(this.templateProvider);
        }

        [Test]
        public void ContentTypeShouldBeTheHtmlMimeType()
        {
            Assert.That(this.converter.ContentType, Is.EqualTo("text/html"));
        }

        [Test]
        public void FormatsShouldIncludeTheHtmlMimeType()
        {
            Assert.That(this.converter.Formats, Has.Member("text/html"));
        }

        [Test]
        public void FormatsShouldIncludeTheXHtmlMimeType()
        {
            Assert.That(this.converter.Formats, Has.Member("application/xhtml+xml"));
        }

        [Test]
        public void ProirityShouldReturnAPositiveNumber()
        {
            Assert.That(this.converter.Priority, Is.Positive);
        }

        [Test]
        public void WriteToMustNotDisposeTheStream()
        {
            Stream stream = Substitute.For<Stream>();
            stream.CanWrite.Returns(true);

            this.converter.WriteTo(stream, "value");

            stream.DidNotReceive().Dispose();
        }

        [Test]
        public void WriteToShouldIncludeTheTemplateParts()
        {
            this.templateProvider.Template.Returns("BeforeAfter");
            this.templateProvider.ContentLocation.Returns("Before".Length);

            string output = this.GetOutput(null);

            Assert.That(output, Does.StartWith("Before"));
            Assert.That(output, Does.EndWith("After"));
        }

        [Test]
        public void WriteToShouldOutputTheStatusCode()
        {
            string output = this.GetOutput(null);

            // This status code will always be 200 for HTML output
            Assert.That(output, Contains.Substring("200"));
        }

        [Test]
        public void WriteToShouldIncludeTheHintText()
        {
            this.templateProvider.HintText.Returns("Hint text");

            string output = this.GetOutput(null);

            Assert.That(output, Contains.Substring("Hint text"));
        }

        [Test]
        public void WriteToShouldWriteStrings()
        {
            // This test is to make sure the IEnumerable logic doesn't interpret
            // strings as IEnumerable<char>
            string output = this.GetOutput("String value");

            Assert.That(output, Contains.Substring("String value"));
        }

        [Test]
        public void WriteToShouldOuputClassProperties()
        {
            string output = this.GetOutput(new SimpleClass { Integer = 123 });

            Assert.That(output, Contains.Substring(nameof(SimpleClass.Integer)));
            Assert.That(output, Contains.Substring("123"));
        }

        [Test]
        public void WriteToShouldOuputNestedClassesProperties()
        {
            var instance = new ComplexClass();
            instance.Nested.Integer = 123;

            string output = this.GetOutput(instance);

            Assert.That(output, Contains.Substring(nameof(ComplexClass.Nested)));
            Assert.That(output, Contains.Substring(nameof(SimpleClass.Integer)));
            Assert.That(output, Contains.Substring("123"));
        }

        [Test]
        public void WriteToShouldOuputLists()
        {
            var instance = new ComplexClass
            {
                StringArray = new[] { "first", "second" }
            };

            string output = this.GetOutput(instance);

            Assert.That(output, Contains.Substring(nameof(ComplexClass.StringArray)));
            Assert.That(output, Contains.Substring("[0]"));
            Assert.That(output, Contains.Substring("first"));
            Assert.That(output, Contains.Substring("[1]"));
            Assert.That(output, Contains.Substring("second"));
        }

        [Test]
        public void WriteToShouldHandleRecursiveProperties()
        {
            var instance = new RecursiveClass
            {
                Decimal = 123.0m,
            };
            instance.Self = instance;

            string output = this.GetOutput(instance);

            Assert.That(output, Contains.Substring(nameof(RecursiveClass.Decimal)));
            Assert.That(output, Contains.Substring("123.0"));
            Assert.That(output, Contains.Substring(nameof(RecursiveClass.Self)));
        }

        private string GetOutput(object obj)
        {
            using (var stream = new MemoryStream())
            {
                this.converter.WriteTo(stream, obj);

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private class ComplexClass
        {
            public SimpleClass Nested { get; } = new SimpleClass();

            public string[] StringArray { get; set; }
        }

        private class RecursiveClass
        {
            public decimal Decimal { get; set; }

            public RecursiveClass Self { get; set; }
        }

        private class SimpleClass
        {
            public int Integer { get; set; }
        }
    }
}
