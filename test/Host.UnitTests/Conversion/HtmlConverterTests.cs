namespace Host.UnitTests.Conversion
{
    using System.IO;
    using System.Text;
    using Crest.Host;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class HtmlConverterTests
    {
        private HtmlConverter converter;
        private IHtmlTemplateProvider templateProvider;

        [SetUp]
        public void SetUp()
        {
            this.templateProvider = Substitute.For<IHtmlTemplateProvider>();
            this.converter = new HtmlConverter(this.templateProvider);
        }

        [TestFixture]
        public sealed class ContentType : HtmlConverterTests
        {
            [Test]
            public void ShouldBeTheHtmlMimeType()
            {
                this.converter.ContentType.Should().Be("text/html");
            }
        }

        [TestFixture]
        public sealed class Formats : HtmlConverterTests
        {
            [Test]
            public void ShouldIncludeTheHtmlMimeType()
            {
                this.converter.Formats.Should().Contain("text/html");
            }

            [Test]
            public void ShouldIncludeTheXHtmlMimeType()
            {
                this.converter.Formats.Should().Contain("application/xhtml+xml");
            }
        }

        [TestFixture]
        public sealed class Proirity : HtmlConverterTests
        {
            [Test]
            public void ShouldReturnAPositiveNumber()
            {
                this.converter.Priority.Should().BePositive();
            }
        }

        [TestFixture]
        public sealed class WriteTo : HtmlConverterTests
        {
            [Test]
            public void MustNotDisposeTheStream()
            {
                Stream stream = Substitute.For<Stream>();
                stream.CanWrite.Returns(true);

                this.converter.WriteTo(stream, "value");

                stream.DidNotReceive().Dispose();
            }

            [Test]
            public void ShouldHandleRecursiveProperties()
            {
                var instance = new RecursiveClass
                {
                    Decimal = 123.0m,
                };
                instance.Self = instance;

                string output = this.GetOutput(instance);

                output.Should().Contain(nameof(RecursiveClass.Decimal));
                output.Should().Contain("123.0");
                output.Should().Contain(nameof(RecursiveClass.Self));
            }

            [Test]
            public void ShouldIncludeTheHintText()
            {
                this.templateProvider.HintText.Returns("Hint text");

                string output = this.GetOutput(null);

                output.Should().Contain("Hint text");
            }

            [Test]
            public void ShouldIncludeTheTemplateParts()
            {
                this.templateProvider.Template.Returns("BeforeAfter");
                this.templateProvider.ContentLocation.Returns("Before".Length);

                string output = this.GetOutput(null);

                output.Should().StartWith("Before");
                output.Should().EndWith("After");
            }

            [Test]
            public void ShouldOuputClassProperties()
            {
                string output = this.GetOutput(new SimpleClass { Integer = 123 });

                output.Should().Contain(nameof(SimpleClass.Integer));
                output.Should().Contain("123");
            }

            [Test]
            public void ShouldOuputLists()
            {
                var instance = new ComplexClass
                {
                    StringArray = new[] { "first", "second" }
                };

                string output = this.GetOutput(instance);

                output.Should().Contain(nameof(ComplexClass.StringArray));
                output.Should().Contain("[0]");
                output.Should().Contain("first");
                output.Should().Contain("[1]");
                output.Should().Contain("second");
            }

            [Test]
            public void ShouldOuputNestedClassesProperties()
            {
                var instance = new ComplexClass();
                instance.Nested.Integer = 123;

                string output = this.GetOutput(instance);

                output.Should().Contain(nameof(ComplexClass.Nested));
                output.Should().Contain(nameof(SimpleClass.Integer));
                output.Should().Contain("123");
            }

            [Test]
            public void ShouldOutputTheStatusCode()
            {
                string output = this.GetOutput(null);

                // This status code will always be 200 for HTML output
                output.Should().Contain("200");
            }

            [Test]
            public void ShouldWriteStrings()
            {
                // This test is to make sure the IEnumerable logic doesn't interpret
                // strings as IEnumerable<char>
                string output = this.GetOutput("String value");

                output.Should().Contain("String value");
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
}
