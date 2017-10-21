namespace OpenApi.Generator.UnitTests
{
    using System;
    using System.Collections;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Reflection;
    using Crest.OpenApi.Generator;
    using FluentAssertions;
    using Newtonsoft.Json;
    using NSubstitute;
    using Xunit;

    public class DefinitionWriterTests
    {
        private readonly XmlDocParser xmlDoc = Substitute.For<XmlDocParser>();

        private DefinitionWriterTests()
        {
            Trace.SetUpTrace("quiet");
        }

        private static dynamic ConvertJson(string value)
        {
            var settings = new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore
            };
            return JsonConvert.DeserializeObject(value, settings);
        }

        public sealed class CreateDefinitionFor : DefinitionWriterTests
        {
            [Fact]
            public void ShouldReturnAReferenceToTheType()
            {
                var writer = new DefinitionWriter(null, null);

                string schema = writer.CreateDefinitionFor(typeof(SimpleClass));
                dynamic result = ConvertJson("{" + schema + "}");

                ((string)result["$ref"]).Should().Be("#/definitions/SimpleClass");
            }

            [Fact]
            public void ShouldReturnArrayDeclarations()
            {
                var writer = new DefinitionWriter(null, null);

                string schema = writer.CreateDefinitionFor(typeof(SimpleClass[]));
                dynamic result = ConvertJson("{" + schema + "}");

                ((string)result.type).Should().Be("array");
                ((string)result.items["$ref"]).Should().Be(@"#/definitions/SimpleClass");
            }
        }

        public sealed class TryGetPrimitive : DefinitionWriterTests
        {
            [Theory]
            [InlineData(typeof(sbyte), "integer", "int8")]
            [InlineData(typeof(short), "integer", "int16")]
            [InlineData(typeof(int), "integer", "int32")]
            [InlineData(typeof(long), "integer", "int64")]
            [InlineData(typeof(byte), "integer", "uint8")]
            [InlineData(typeof(ushort), "integer", "uint16")]
            [InlineData(typeof(uint), "integer", "uint32")]
            [InlineData(typeof(ulong), "integer", "uint64")]
            [InlineData(typeof(float), "number", "float")]
            [InlineData(typeof(double), "number", "double")]
            [InlineData(typeof(bool), "boolean", null)]
            [InlineData(typeof(string), "string", null)]
            [InlineData(typeof(DateTime), "string", "date-time")]
            [InlineData(typeof(byte[]), "string", "byte")]
            [InlineData(typeof(Guid), "string", "uuid")]
            public void ShouldHandleKnownPrimitives(Type netType, string type, string format)
            {
                var writer = new DefinitionWriter(null, null);

                bool result = writer.TryGetPrimitive(netType, out string primitive);
                dynamic definition = JsonConvert.DeserializeObject("{" + primitive + "}");

                result.Should().BeTrue();
                ((string)definition.type).Should().Be(type);
                ((string)definition.format).Should().Be(format);
            }
        }

        public sealed class WriteDefinitions : DefinitionWriterTests
        {
            [Fact]
            public void ShouldIncludeMaxLengthAttributesForArrays()
            {
                dynamic result = this.GetDefinitionFor<LengthProperties>();
                dynamic property = result.properties.maxLength;

                ((int)property.maxItems).Should().Be(10);
            }

            [Fact]
            public void ShouldIncludeMaxLengthAttributesForStrings()
            {
                dynamic result = this.GetDefinitionFor<LengthProperties>();
                dynamic property = result.properties.minMaxString;

                ((int)property.maxLength).Should().Be(10);
            }

            [Fact]
            public void ShouldIncludeMinLengthAttributesForArrays()
            {
                dynamic result = this.GetDefinitionFor<LengthProperties>();
                dynamic property = result.properties.minLength;

                ((int)property.minItems).Should().Be(5);
            }

            [Fact]
            public void ShouldIncludeMinLengthAttributesForStrings()
            {
                dynamic result = this.GetDefinitionFor<LengthProperties>();
                dynamic property = result.properties.minMaxString;

                ((int)property.minLength).Should().Be(5);
            }

            [Fact]
            public void ShouldIncludeNonPrimitiveProperties()
            {
                dynamic result = this.GetDefinitionFor<ComplexClass>();

                ((string)result.properties.simpleProperty["$ref"]).Should().Be("#/definitions/SimpleClass");
            }

            [Fact]
            public void ShouldIncludeStringLengthAttributes()
            {
                dynamic result = this.GetDefinitionFor<LengthProperties>();
                dynamic stringLength = result.properties.stringLength;

                ((int)stringLength.minLength).Should().Be(5);
                ((int)stringLength.maxLength).Should().Be(10);
            }

            [Fact]
            public void ShouldNotWriteEmptyRequiredArrays()
            {
                // The minimum size for the required array is 1, so can't have
                // empty arrays...
                dynamic result = this.GetDefinitionFor<SimpleClass>();

                ((object)result.required).Should().BeNull();
            }

            [Fact]
            public void ShouldWriteCamelCaseProperties()
            {
                dynamic result = this.GetDefinitionFor<SimpleClass>();

                ((string)result.type).Should().Be("object");
                ((string)result.properties.myProperty.type).Should().Be("string");
            }

            [Fact]
            public void ShouldWriteRequiredProperties()
            {
                dynamic result = this.GetDefinitionFor<RequiredProperties>();

                ((IEnumerable)result.required).Should().HaveCount(1);
                ((string)result.required[0]).Should().Be("isRequired");
            }

            [Fact]
            public void ShouldWriteTheClassSummary()
            {
                this.xmlDoc.GetClassDescription(typeof(SimpleClass))
                    .Returns(new ClassDescription { Summary = "Class summary" });

                dynamic result = this.GetDefinitionFor<SimpleClass>();

                ((string)result.description).Should().Be("Class summary");
            }

            [Fact]
            public void ShouldWriteThePropertySummary()
            {
                PropertyInfo property = typeof(SimpleClass).GetProperty(nameof(SimpleClass.MyProperty));
                this.xmlDoc.GetDescription(property).Returns("Property summary");

                dynamic result = this.GetDefinitionFor<SimpleClass>();

                ((string)result.properties.myProperty.description).Should().Be("Property summary");
            }

            [Fact]
            public void WriteDefinitionsShouldIncludeArrayProperties()
            {
                dynamic result = this.GetDefinitionFor<ComplexClass>();
                dynamic arrayProperty = result.properties.arrayProperty;

                ((string)arrayProperty.type).Should().Be("array");
                ((string)arrayProperty.items.type).Should().Be("string");
            }

            private dynamic GetDefinitionFor<T>()
            {
                using (var stringWriter = new StringWriter())
                {
                    var writer = new DefinitionWriter(this.xmlDoc, stringWriter);
                    writer.CreateDefinitionFor(typeof(T));
                    writer.WriteDefinitions();
                    dynamic result = ConvertJson(stringWriter.ToString());
                    return result[typeof(T).Name];
                }
            }

            private class ComplexClass
            {
                public string[] ArrayProperty { get; set; }

                public SimpleClass SimpleProperty { get; set; }
            }

            private class LengthProperties
            {
                [MaxLength(10)]
                public int[] MaxLength { get; set; }

                [MinLength(5)]
                public int[] MinLength { get; set; }

                [MinLength(5)]
                [MaxLength(10)]
                public string MinMaxString { get; set; }

                [StringLength(10, MinimumLength = 5)]
                public string StringLength { get; set; }
            }

            private class RequiredProperties
            {
                [Required]
                public string IsRequired { get; set; }

                public string NotRequired { get; set; }
            }
        }

        private class SimpleClass
        {
            public string MyProperty { get; set; }
        }
    }
}
