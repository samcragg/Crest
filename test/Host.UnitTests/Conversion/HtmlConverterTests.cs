namespace Host.UnitTests.Conversion
{
    using System;
    using System.IO;
    using System.Text;
    using Crest.Abstractions;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class HtmlConverterTests
    {
        private readonly HtmlConverter converter;
        private readonly IHtmlTemplateProvider templateProvider;

        public HtmlConverterTests()
        {
            this.templateProvider = Substitute.For<IHtmlTemplateProvider>();
            this.converter = new HtmlConverter(this.templateProvider);
        }

        public sealed class ContentType : HtmlConverterTests
        {
            [Fact]
            public void ShouldBeTheHtmlMimeType()
            {
                this.converter.ContentType.Should().Be("text/html");
            }
        }

        public sealed class Formats : HtmlConverterTests
        {
            [Fact]
            public void ShouldIncludeTheHtmlMimeType()
            {
                this.converter.Formats.Should().Contain("text/html");
            }

            [Fact]
            public void ShouldIncludeTheXHtmlMimeType()
            {
                this.converter.Formats.Should().Contain("application/xhtml+xml");
            }
        }

        public sealed class Prime : HtmlConverterTests
        {
            [Fact]
            public void ShouldIgnoreTheParameter()
            {
                Action action = () => this.converter.Prime(null);

                action.ShouldNotThrow();
            }
        }

        public sealed class Proirity : HtmlConverterTests
        {
            [Fact]
            public void ShouldReturnAPositiveNumber()
            {
                this.converter.Priority.Should().BePositive();
            }
        }

        public sealed class WriteTo : HtmlConverterTests
        {
            [Fact]
            public void MustNotDisposeTheStream()
            {
                Stream stream = Substitute.For<Stream>();
                stream.CanWrite.Returns(true);

                this.converter.WriteTo(stream, "value");

                stream.DidNotReceive().Dispose();
            }

            [Fact]
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

            [Fact]
            public void ShouldIncludeTheHintText()
            {
                this.templateProvider.HintText.Returns("Hint text");

                string output = this.GetOutput(null);

                output.Should().Contain("Hint text");
            }

            [Fact]
            public void ShouldIncludeTheTemplateParts()
            {
                this.templateProvider.Template.Returns("BeforeAfter");
                this.templateProvider.ContentLocation.Returns("Before".Length);

                string output = this.GetOutput(null);

                output.Should().StartWith("Before");
                output.Should().EndWith("After");
            }

            [Fact]
            public void ShouldOuputClassProperties()
            {
                string output = this.GetOutput(new SimpleClass { Integer = 123 });

                output.Should().Contain(nameof(SimpleClass.Integer));
                output.Should().Contain("123");
            }

            [Fact]
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

            [Fact]
            public void ShouldOuputNestedClassesProperties()
            {
                var instance = new ComplexClass();
                instance.Nested.Integer = 123;

                string output = this.GetOutput(instance);

                output.Should().Contain(nameof(ComplexClass.Nested));
                output.Should().Contain(nameof(SimpleClass.Integer));
                output.Should().Contain("123");
            }

            [Fact]
            public void ShouldOutputTheStatusCode()
            {
                string output = this.GetOutput(null);

                // This status code will always be 200 for HTML output
                output.Should().Contain("200");
            }

            [Fact]
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
