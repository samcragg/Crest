namespace OpenApi.UnitTests
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.OpenApi;
    using Newtonsoft.Json;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class OperationObjectWriterTests
    {
        private static readonly MethodInfo HasReturnMethod = typeof(ExampleMethods).GetMethod(nameof(ExampleMethods.HasReturn));
        private static readonly MethodInfo NoParameterMethod = typeof(ExampleMethods).GetMethod(nameof(ExampleMethods.NoParameter));
        private static readonly MethodInfo ObsoleteMethod = typeof(ExampleMethods).GetMethod(nameof(ExampleMethods.Obsolete));
        private static readonly MethodInfo SingleParameterMethod = typeof(ExampleMethods).GetMethod(nameof(ExampleMethods.SingleParameter));
        private XmlDocParser xmlDoc;

        [SetUp]
        public void SetUp()
        {
            this.xmlDoc = Substitute.For<XmlDocParser>();
        }

        [Test]
        public void ShouldWriteTheTags()
        {
            dynamic result = this.GetOutput("/route", NoParameterMethod);

            Assert.That((string)result.tags[0], Is.EqualTo(nameof(ExampleMethods)));
        }

        [Test]
        public void ShouldWriteTheSummaryWithoutTrailingFullstop()
        {
            this.xmlDoc.GetMethodDescription(NoParameterMethod)
                .Returns(new MethodDescription { Summary = "Summary text." });

            dynamic result = this.GetOutput("/route", NoParameterMethod);

            Assert.That((string)result.summary, Is.EqualTo("Summary text"));
        }

        [Test]
        public void ShouldWriteTheDescription()
        {
            this.xmlDoc.GetMethodDescription(NoParameterMethod)
                .Returns(new MethodDescription { Remarks = "Remarks." });

            dynamic result = this.GetOutput("/route", NoParameterMethod);

            Assert.That((string)result.description, Is.EqualTo("Remarks."));
        }

        [Test]
        public void ShouldWriteTheOperationId()
        {
            dynamic result = this.GetOutput("/route", NoParameterMethod);

            Assert.That((string)result.operationId, Contains.Substring(nameof(ExampleMethods.NoParameter)));
        }

        [Test]
        public void ShouldWritePathParameters()
        {
            var description = new MethodDescription();
            description.Parameters.Add("parameter", "Summary text.");
            this.xmlDoc.GetMethodDescription(SingleParameterMethod)
                .Returns(description);

            dynamic result = this.GetOutput("/{parameter}", SingleParameterMethod);

            Assert.That(result.parameters, Has.Count.EqualTo(1));
            Assert.That((string)result.parameters[0].name, Is.EqualTo("parameter"));
            Assert.That((string)result.parameters[0].@in, Is.EqualTo("path"));
            Assert.That((string)result.parameters[0].description, Is.EqualTo("Summary text."));
        }

        [Test]
        public void ShouldWriteQueryParameters()
        {
            var description = new MethodDescription();
            description.Parameters.Add("parameter", "Summary text.");
            this.xmlDoc.GetMethodDescription(SingleParameterMethod)
                .Returns(description);

            dynamic result = this.GetOutput("/route?queryKey={parameter}", SingleParameterMethod);

            Assert.That(result.parameters, Has.Count.EqualTo(1));
            Assert.That((string)result.parameters[0].name, Is.EqualTo("queryKey"));
            Assert.That((string)result.parameters[0].@in, Is.EqualTo("query"));
            Assert.That((string)result.parameters[0].description, Is.EqualTo("Summary text."));
        }

        [Test]
        public void ShouldWriteTheCorrectResponseForTaskMethods()
        {
            this.xmlDoc.GetMethodDescription(NoParameterMethod)
                .Returns(new MethodDescription { Returns = "Returns text." });

            dynamic result = this.GetOutput("/route", NoParameterMethod);

            Assert.That((string)result.responses["204"].description, Is.EqualTo("Returns text."));
            Assert.That(result.responses["204"].schema, Is.Null);
        }

        [Test]
        public void ShouldWriteTheCorrectResponseForGenericTaskMethods()
        {
            this.xmlDoc.GetMethodDescription(HasReturnMethod)
                .Returns(new MethodDescription { Returns = "Returns text." });

            dynamic result = this.GetOutput("/route", HasReturnMethod);

            Assert.That((string)result.responses["200"].description, Is.EqualTo("Returns text."));
            Assert.That(result.responses["200"].schema, Is.Not.Null);
        }

        [Test]
        public void ShouldOutputDepricatedForObsoleteMethods()
        {
            dynamic result = this.GetOutput("/route", ObsoleteMethod);

            Assert.That((bool)result.deprecated, Is.True);
        }

        private dynamic GetOutput(string route, MethodInfo method)
        {
            using (var stringWriter = new StringWriter())
            {
                var pathItemWriter = new OperationObjectWriter(
                    this.xmlDoc,
                    new DefinitionWriter(null),
                    new TagWriter(this.xmlDoc, null),
                    stringWriter);

                pathItemWriter.WriteOperation(route, method);
                return JsonConvert.DeserializeObject(stringWriter.ToString());
            }
        }

        private class ExampleMethods
        {
            public Task<int> HasReturn()
            {
                return Task.FromResult(0);
            }

            public Task NoParameter()
            {
                return Task.CompletedTask;
            }

            public Task SingleParameter(string parameter)
            {
                return Task.CompletedTask;
            }

            [Obsolete]
            public Task Obsolete()
            {
                return Task.CompletedTask;
            }
        }
    }
}
