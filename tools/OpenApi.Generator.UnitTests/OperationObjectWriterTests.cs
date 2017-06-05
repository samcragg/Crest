namespace OpenApi.Generator.UnitTests
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.OpenApi.Generator;
    using FluentAssertions;
    using Newtonsoft.Json;
    using NSubstitute;
    using Xunit;

    public class OperationObjectWriterTests
    {
        private static readonly MethodInfo HasReturnMethod = typeof(ExampleMethods).GetMethod(nameof(ExampleMethods.HasReturn));
        private static readonly MethodInfo NoParameterMethod = typeof(ExampleMethods).GetMethod(nameof(ExampleMethods.NoParameter));
        private static readonly MethodInfo ObsoleteMethod = typeof(ExampleMethods).GetMethod(nameof(ExampleMethods.Obsolete));
        private static readonly MethodInfo SingleParameterMethod = typeof(ExampleMethods).GetMethod(nameof(ExampleMethods.SingleParameter));
        private readonly XmlDocParser xmlDoc = Substitute.For<XmlDocParser>();

        private dynamic GetOutput(string route, MethodInfo method)
        {
            using (var stringWriter = new StringWriter())
            {
                var pathItemWriter = new OperationObjectWriter(
                    this.xmlDoc,
                    new DefinitionWriter(null, null),
                    new TagWriter(this.xmlDoc, null),
                    stringWriter);

                pathItemWriter.WriteOperation(route, method);
                return JsonConvert.DeserializeObject(stringWriter.ToString());
            }
        }

        public sealed class WriteOperation : OperationObjectWriterTests
        {
            [Fact]
            public void ShouldOutputDepricatedForObsoleteMethods()
            {
                dynamic result = this.GetOutput("/route", ObsoleteMethod);

                ((bool)result.deprecated).Should().BeTrue();
            }

            [Fact]
            public void ShouldWritePathParameters()
            {
                var description = new MethodDescription();
                description.Parameters.Add("parameter", "Summary text.");
                this.xmlDoc.GetMethodDescription(SingleParameterMethod)
                    .Returns(description);

                dynamic result = this.GetOutput("/{parameter}", SingleParameterMethod);

                ((IEnumerable)result.parameters).Should().HaveCount(1);
                ((string)result.parameters[0].name).Should().Be("parameter");
                ((string)result.parameters[0].@in).Should().Be("path");
                ((string)result.parameters[0].description).Should().Be("Summary text.");
            }

            [Fact]
            public void ShouldWriteQueryParameters()
            {
                var description = new MethodDescription();
                description.Parameters.Add("parameter", "Summary text.");
                this.xmlDoc.GetMethodDescription(SingleParameterMethod)
                    .Returns(description);

                dynamic result = this.GetOutput("/route?queryKey={parameter}", SingleParameterMethod);

                ((IEnumerable)result.parameters).Should().HaveCount(1);
                ((string)result.parameters[0].name).Should().Be("queryKey");
                ((string)result.parameters[0].@in).Should().Be("query");
                ((string)result.parameters[0].description).Should().Be("Summary text.");
            }

            [Fact]
            public void ShouldWriteTheCorrectResponseForGenericTaskMethods()
            {
                this.xmlDoc.GetMethodDescription(HasReturnMethod)
                    .Returns(new MethodDescription { Returns = "Returns text." });

                dynamic result = this.GetOutput("/route", HasReturnMethod);

                ((string)result.responses["200"].description).Should().Be("Returns text.");
                ((object)result.responses["200"].schema).Should().NotBeNull();
            }

            [Fact]
            public void ShouldWriteTheCorrectResponseForTaskMethods()
            {
                this.xmlDoc.GetMethodDescription(NoParameterMethod)
                    .Returns(new MethodDescription { Returns = "Returns text." });

                dynamic result = this.GetOutput("/route", NoParameterMethod);

                ((string)result.responses["204"].description).Should().Be("Returns text.");
                ((object)result.responses["204"].schema).Should().BeNull();
            }

            [Fact]
            public void ShouldWriteTheDescription()
            {
                this.xmlDoc.GetMethodDescription(NoParameterMethod)
                    .Returns(new MethodDescription { Remarks = "Remarks." });

                dynamic result = this.GetOutput("/route", NoParameterMethod);

                ((string)result.description).Should().Be("Remarks.");
            }

            [Fact]
            public void ShouldWriteTheOperationId()
            {
                dynamic result = this.GetOutput("/route", NoParameterMethod);

                ((string)result.operationId).Should().Contain(nameof(ExampleMethods.NoParameter));
            }

            [Fact]
            public void ShouldWriteTheSummaryWithoutTrailingFullstop()
            {
                this.xmlDoc.GetMethodDescription(NoParameterMethod)
                    .Returns(new MethodDescription { Summary = "Summary text." });

                dynamic result = this.GetOutput("/route", NoParameterMethod);

                ((string)result.summary).Should().Be("Summary text");
            }

            [Fact]
            public void ShouldWriteTheTags()
            {
                dynamic result = this.GetOutput("/route", NoParameterMethod);

                ((string)result.tags[0]).Should().Be(nameof(ExampleMethods));
            }

            [Fact]
            public void ShouldWriteUniqueOperationIds()
            {
                using (var stringWriter = new StringWriter())
                {
                    var pathItemWriter = new OperationObjectWriter(
                        this.xmlDoc,
                        new DefinitionWriter(null, null),
                        new TagWriter(this.xmlDoc, null),
                        stringWriter);

                    stringWriter.Write('[');
                    pathItemWriter.WriteOperation("/route1", NoParameterMethod);
                    stringWriter.Write(',');
                    pathItemWriter.WriteOperation("/route2", NoParameterMethod);
                    stringWriter.Write(']');

                    dynamic result = JsonConvert.DeserializeObject(stringWriter.ToString());

                    ((string)result[0].operationId).Should().NotBe((string)result[1].operationId);
                }
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

            [Obsolete]
            public Task Obsolete()
            {
                return Task.CompletedTask;
            }

            public Task SingleParameter(string parameter)
            {
                return Task.CompletedTask;
            }
        }
    }
}
