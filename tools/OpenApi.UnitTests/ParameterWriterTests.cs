namespace OpenApi.UnitTests
{
    using System;
    using System.IO;
    using System.Reflection;
    using Crest.OpenApi;
    using Newtonsoft.Json;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ParameterWriterTests
    {
        [Test]
        public void WritePathParameterShouldWriteTheName()
        {
            ParameterInfo parameter = CreateParameter("parameterName", typeof(string));

            dynamic result = this.GetOutput(w => w.WritePathParameter(parameter, ""));

            Assert.That((string)result.name, Is.EqualTo("parameterName"));
        }

        [Test]
        public void WritePathParameterShouldSetInToPath()
        {
            ParameterInfo parameter = CreateParameter("", typeof(string));

            dynamic result = this.GetOutput(w => w.WritePathParameter(parameter, ""));

            Assert.That((string)result.@in, Is.EqualTo("path"));
        }

        [Test]
        public void WritePathParameterShouldWriteTheDescription()
        {
            ParameterInfo parameter = CreateParameter("", typeof(string));

            dynamic result = this.GetOutput(w => w.WritePathParameter(parameter, "Description text"));

            Assert.That((string)result.description, Is.EqualTo("Description text"));
        }

        [Test]
        public void WritePathParameterShouldSetRequiredToTrue()
        {
            ParameterInfo parameter = CreateParameter("", typeof(string));

            dynamic result = this.GetOutput(w => w.WritePathParameter(parameter, ""));

            Assert.That((bool)result.required, Is.True);
        }

        [Test]
        public void WriteParameterShouldWriteArrays()
        {
            ParameterInfo parameter = CreateParameter("", typeof(int[]));

            dynamic result = this.GetOutput(w => w.WritePathParameter(parameter, ""));

            Assert.That((string)result.type, Is.EqualTo("array"));
            Assert.That((string)result.items.type, Is.EqualTo("integer"));
            Assert.That((string)result.items.format, Is.EqualTo("int32"));
        }

        [Test]
        public void WriteQueryParameterShouldWriteTheQueryKey()
        {
            ParameterInfo parameter = CreateParameter("parameterName", typeof(string));

            dynamic result = this.GetOutput(w => w.WriteQueryParameter(parameter, "queryKey", ""));

            Assert.That((string)result.name, Is.EqualTo("queryKey"));
        }

        [Test]
        public void WriteQueryParameterShouldSetInToQuery()
        {
            ParameterInfo parameter = CreateParameter("", typeof(string));

            dynamic result = this.GetOutput(w => w.WriteQueryParameter(parameter, "", ""));

            Assert.That((string)result.@in, Is.EqualTo("query"));
        }

        [Test]
        public void WriteQueryParameterShouldWriteTheDescription()
        {
            ParameterInfo parameter = CreateParameter("", typeof(string));

            dynamic result = this.GetOutput(w => w.WriteQueryParameter(parameter, "", "Description text"));

            Assert.That((string)result.description, Is.EqualTo("Description text"));
        }

        [Test]
        public void WriteQueryParameterShouldSetAllowEmptyValueForBoolParameters()
        {
            ParameterInfo parameter = CreateParameter("", typeof(bool));

            dynamic result = this.GetOutput(w => w.WriteQueryParameter(parameter, "", ""));

            Assert.That((string)result.type, Is.EqualTo("boolean"));
            Assert.That((bool)result.allowEmptyValue, Is.True);
        }

        [Test]
        public void WriteQueryParameterShouldSetTheDefaultValue()
        {
            ParameterInfo parameter = CreateParameter("", typeof(int));
            parameter.HasDefaultValue.Returns(true);
            parameter.RawDefaultValue.Returns(123);

            dynamic result = this.GetOutput(w => w.WriteQueryParameter(parameter, "", ""));

            Assert.That((int)result.@default, Is.EqualTo(123));
        }

        private ParameterInfo CreateParameter(string name, Type type)
        {
            var parameter = Substitute.For<ParameterInfo>();
            parameter.Name.Returns(name);
            parameter.ParameterType.Returns(type);
            return parameter;
        }

        private dynamic GetOutput(Action<ParameterWriter> action)
        {
            using (var stringWriter = new StringWriter())
            {
                var parameterWriter = new ParameterWriter(new DefinitionWriter(null), stringWriter);
                action(parameterWriter);
                return JsonConvert.DeserializeObject(stringWriter.ToString());
            }
        }
    }
}
