namespace OpenApi.UnitTests
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using Crest.OpenApi;
    using Newtonsoft.Json;
    using NUnit.Framework;

    [TestFixture]
    public sealed class DefinitionWriterTests
    {
        [Test]
        public void CreateDefinitionForShouldReturnAReferenceToTheType()
        {
            var writer = new DefinitionWriter(null);

            string schema = writer.CreateDefinitionFor(typeof(SimpleClass));
            dynamic result = ConvertJson("{" + schema + "}");

            Assert.That((string)result["$ref"], Is.EqualTo("#/definitions/SimpleClass"));
        }

        [Test]
        public void CreateDefinitionForShouldReturnArrayDeclarations()
        {
            var writer = new DefinitionWriter(null);

            string schema = writer.CreateDefinitionFor(typeof(SimpleClass[]));
            dynamic result = ConvertJson("{" + schema + "}");

            Assert.That((string)result.type, Is.EqualTo("array"));
            Assert.That((string)result.items["$ref"], Is.EqualTo(@"#/definitions/SimpleClass"));
        }

        [TestCase(typeof(sbyte), "integer", "int8")]
        [TestCase(typeof(short), "integer", "int16")]
        [TestCase(typeof(int), "integer", "int32")]
        [TestCase(typeof(long), "integer", "int64")]
        [TestCase(typeof(byte), "integer", "uint8")]
        [TestCase(typeof(ushort), "integer", "uint16")]
        [TestCase(typeof(uint), "integer", "uint32")]
        [TestCase(typeof(ulong), "integer", "uint64")]
        [TestCase(typeof(float), "number", "float")]
        [TestCase(typeof(double), "number", "double")]
        [TestCase(typeof(bool), "boolean", null)]
        [TestCase(typeof(string), "string", null)]
        [TestCase(typeof(DateTime), "string", "date-time")]
        [TestCase(typeof(byte[]), "string", "byte")]
        [TestCase(typeof(Guid), "string", "uuid")]
        public void TryGetPrimitiveShouldHandleKnownPrimitives(Type netType, string type, string format)
        {
            var writer = new DefinitionWriter(null);

            string primitive;
            bool result = writer.TryGetPrimitive(netType, out primitive);
            dynamic definition = JsonConvert.DeserializeObject("{" + primitive + "}");

            Assert.That(result, Is.True);
            Assert.That((string)definition.type, Is.EqualTo(type));
            Assert.That((string)definition.format, Is.EqualTo(format));
        }

        [Test]
        public void WriteDefinitionsShouldWriteCamelCaseProperties()
        {
            dynamic result = this.GetDefinitionFor<SimpleClass>();

            Assert.That((string)result.type, Is.EqualTo("object"));
            Assert.That((string)result.properties.myProperty.type, Is.EqualTo("string"));
        }

        [Test]
        public void WriteDefinitionsShouldWriteRequiredProperties()
        {
            dynamic result = this.GetDefinitionFor<RequiredProperties>();

            Assert.That(result.required, Has.Count.EqualTo(1));
            Assert.That((string)result.required[0], Is.EqualTo("isRequired"));
        }

        [Test]
        public void WriteDefinitionsShouldIncludeMaxLengthAttributesForArrays()
        {
            dynamic result = this.GetDefinitionFor<LengthProperties>();
            dynamic property = result.properties.maxLength;

            Assert.That((int)property.maxItems, Is.EqualTo(10));
        }

        [Test]
        public void WriteDefinitionsShouldIncludeMaxLengthAttributesForStrings()
        {
            dynamic result = this.GetDefinitionFor<LengthProperties>();
            dynamic property = result.properties.minMaxString;

            Assert.That((int)property.maxLength, Is.EqualTo(10));
        }

        [Test]
        public void WriteDefinitionsShouldIncludeMinLengthAttributesForArrays()
        {
            dynamic result = this.GetDefinitionFor<LengthProperties>();
            dynamic property = result.properties.minLength;

            Assert.That((int)property.minItems, Is.EqualTo(5));
        }

        [Test]
        public void WriteDefinitionsShouldIncludeMinLengthAttributesForStrings()
        {
            dynamic result = this.GetDefinitionFor<LengthProperties>();
            dynamic property = result.properties.minMaxString;

            Assert.That((int)property.minLength, Is.EqualTo(5));
        }

        [Test]
        public void WriteDefinitionsShouldIncludeStringLengthAttributes()
        {
            dynamic result = this.GetDefinitionFor<LengthProperties>();
            dynamic stringLength = result.properties.stringLength;

            Assert.That((int)stringLength.minLength, Is.EqualTo(5));
            Assert.That((int)stringLength.maxLength, Is.EqualTo(10));
        }

        [Test]
        public void WriteDefinitionsShouldIncludeArrayProperties()
        {
            dynamic result = this.GetDefinitionFor<ComplexClass>();
            dynamic arrayProperty = result.properties.arrayProperty;

            Assert.That((string)arrayProperty.type, Is.EqualTo("array"));
            Assert.That((string)arrayProperty.items.type, Is.EqualTo("string"));
        }

        [Test]
        public void WriteDefinitionsShouldIncludeNonPrimitiveProperties()
        {
            dynamic result = this.GetDefinitionFor<ComplexClass>();

            Assert.That((string)result.properties.simpleProperty["$ref"], Is.EqualTo("#/definitions/SimpleClass"));
        }

        private static dynamic ConvertJson(string value)
        {
            var settings = new JsonSerializerSettings();
            settings.MetadataPropertyHandling = MetadataPropertyHandling.Ignore;
            return JsonConvert.DeserializeObject(value, settings);
        }

        private dynamic GetDefinitionFor<T>()
        {
            using (var stringWriter = new StringWriter())
            {
                var writer = new DefinitionWriter(stringWriter);
                writer.CreateDefinitionFor(typeof(T));
                writer.WriteDefinitions();
                dynamic result = ConvertJson(stringWriter.ToString());
                return result[typeof(T).Name];
            }
        }

        private class SimpleClass
        {
            public string MyProperty { get; set; }
        }

        private class ComplexClass
        {
            public string[] ArrayProperty { get; set; }

            public SimpleClass SimpleProperty { get; set; }
        }

        private class RequiredProperties
        {
            public string NotRequired { get; set; }

            [Required]
            public string IsRequired { get; set; }
        }

        private class LengthProperties
        {
            [MaxLength(10)]
            public int[] MaxLength { get; set; }

            [MinLength(5)]
            public int[] MinLength { get; set; }

            [StringLength(10, MinimumLength = 5)]
            public string StringLength { get; set; }

            [MinLength(5)]
            [MaxLength(10)]
            public string MinMaxString { get; set; }
        }
    }
}
