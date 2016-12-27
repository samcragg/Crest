namespace OpenApi.UnitTests
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Linq;
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

            string result = writer.CreateDefinitionFor(typeof(DefinitionWriterTests));

            Assert.That(result, Is.EqualTo("#/definitions/DefinitionWriterTests"));
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
        public void WriteDefinitionsShouldWriteArrays()
        {
            dynamic result = this.GetDefinitionFor<SimpleClass[]>();

            Assert.That((string)result.type, Is.EqualTo("array"));
            Assert.That((string)result.items["$ref"], Is.EqualTo(@"#/definitions/SimpleClass"));
        }

        [Test]
        public void WriteDefinitionsShouldWriteRequiredProperties()
        {
            dynamic result = this.GetDefinitionFor<RequiredProperties>();

            Assert.That(result.required, Has.Count.EqualTo(1));
            Assert.That((string)result.required[0], Is.EqualTo("isRequired"));
        }

        private dynamic GetDefinitionFor<T>()
        {
            using (var stringWriter = new StringWriter())
            {
                var writer = new DefinitionWriter(stringWriter);
                writer.CreateDefinitionFor(typeof(T));
                writer.WriteDefinitions();
                dynamic result = JsonConvert.DeserializeObject("{" + stringWriter.ToString() + "}");
                return result.definitions[typeof(T).Name];
            }
        }

        private class SimpleClass
        {
            public string MyProperty { get; set; }
        }

        private class RequiredProperties
        {
            public string NotRequired { get; set; }

            [Required]
            public string IsRequired { get; set; }
        }
    }
}
