namespace OpenApi.UnitTests
{
    using System.Reflection;
    using Crest.OpenApi;
    using FluentAssertions;
    using Xunit;

    public class XmlDocParserTests
    {
        private readonly XmlDocParser parser;

        public XmlDocParserTests()
        {
            Assembly assembly = typeof(XmlDocParserTests).GetTypeInfo().Assembly;
            this.parser = new XmlDocParser(assembly.GetManifestResourceStream("OpenApi.UnitTests.ExampleClass.xml"));
        }

        public string Property { get; set; }

        public sealed class GetClassDescription : XmlDocParserTests
        {
            [Fact]
            public void ShouldReturnEmptyForUnknownTypes()
            {
                ClassDescription result = this.parser.GetClassDescription(typeof(XmlDocParserTests));

                result.Should().NotBeNull();
                result.Remarks.Should().BeNull();
                result.Summary.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheRemarks()
            {
                ClassDescription result = this.parser.GetClassDescription(typeof(ExampleClass));

                result.Remarks.Should().Be("Remarks for the class.");
            }

            [Fact]
            public void ShouldReturnTheSummmary()
            {
                ClassDescription result = this.parser.GetClassDescription(typeof(ExampleClass));

                result.Summary.Should().Be("Summary for the class.");
            }
        }

        public sealed class GetDescription : XmlDocParserTests
        {
            [Fact]
            public void ShouldGetThePropertySummary()
            {
                PropertyInfo property = typeof(ExampleClass).GetProperty(nameof(ExampleClass.Property));

                string description = this.parser.GetDescription(property);

                description.Should().Be("The summary for the property.");
            }

            [Fact]
            public void ShouldRemoveGetsFromTheSummary()
            {
                PropertyInfo property = typeof(ExampleClass).GetProperty(nameof(ExampleClass.GetProperty));

                string description = this.parser.GetDescription(property);

                description.Should().Be("The summary for the property.");
            }

            [Fact]
            public void ShouldRemoveGetsOrSetsFromTheSummary()
            {
                PropertyInfo property = typeof(ExampleClass).GetProperty(nameof(ExampleClass.GetSetProperty));

                string description = this.parser.GetDescription(property);

                description.Should().Be("The summary for the property.");
            }

            [Fact]
            public void ShouldReturnNullForUnknownProperties()
            {
                PropertyInfo property = typeof(XmlDocParserTests).GetProperty(nameof(XmlDocParserTests.Property));

                string description = this.parser.GetDescription(property);

                description.Should().BeNull();
            }
        }

        public sealed class GetMethodDescription : XmlDocParserTests
        {
            [Fact]
            public void ShouldHandleParameterlessMethods()
            {
                MethodInfo method = typeof(ExampleClass).GetMethod(nameof(ExampleClass.MethodWithoutParameter));

                MethodDescription result = this.parser.GetMethodDescription(method);

                result.Summary.Should().Be("Summary for the method.");
                result.Parameters.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnEmptyForUnknownTypes()
            {
                ClassDescription result = this.parser.GetClassDescription(typeof(XmlDocParserTests));

                result.Should().NotBeNull();
                result.Remarks.Should().BeNull();
                result.Summary.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnTheParameters()
            {
                MethodInfo method = typeof(ExampleClass).GetMethod(nameof(ExampleClass.Method));

                MethodDescription result = this.parser.GetMethodDescription(method);

                result.Parameters.Should().HaveCount(1);
                result.Parameters["parameter"].Should().Be("Parameter description.");
            }

            [Fact]
            public void ShouldReturnTheRemarks()
            {
                MethodInfo method = typeof(ExampleClass).GetMethod(nameof(ExampleClass.Method));

                MethodDescription result = this.parser.GetMethodDescription(method);

                result.Remarks.Should().Be("Remarks for the method.");
            }

            [Fact]
            public void ShouldReturnTheReturns()
            {
                MethodInfo method = typeof(ExampleClass).GetMethod(nameof(ExampleClass.Method));

                MethodDescription result = this.parser.GetMethodDescription(method);

                result.Returns.Should().Be("Return description for the method.");
            }

            [Fact]
            public void ShouldReturnTheSummmary()
            {
                MethodInfo method = typeof(ExampleClass).GetMethod(nameof(ExampleClass.Method));

                MethodDescription result = this.parser.GetMethodDescription(method);

                result.Summary.Should().Be("Summary for the method.");
            }
        }
    }
}
