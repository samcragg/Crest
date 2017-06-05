namespace OpenApi.Generator.UnitTests
{
    using System;
    using System.IO;
    using System.Reflection;
    using Crest.OpenApi.Generator;
    using FluentAssertions;
    using Newtonsoft.Json;
    using NSubstitute;
    using Xunit;

    public class ParameterWriterTests
    {
        private ParameterInfo CreateParameter(string name, Type type)
        {
            ParameterInfo parameter = Substitute.For<ParameterInfo>();
            parameter.Name.Returns(name);
            parameter.ParameterType.Returns(type);
            return parameter;
        }

        private dynamic GetOutput(Action<ParameterWriter> action)
        {
            using (var stringWriter = new StringWriter())
            {
                var parameterWriter = new ParameterWriter(new DefinitionWriter(null, null), stringWriter);
                action(parameterWriter);
                return JsonConvert.DeserializeObject(stringWriter.ToString());
            }
        }

        public sealed class WritePathParameter : ParameterWriterTests
        {
            [Fact]
            public void ShouldSetInToPath()
            {
                ParameterInfo parameter = CreateParameter("", typeof(string));

                dynamic result = this.GetOutput(w => w.WritePathParameter(parameter, ""));

                ((string)result.@in).Should().Be("path");
            }

            [Fact]
            public void ShouldSetRequiredToTrue()
            {
                ParameterInfo parameter = CreateParameter("", typeof(string));

                dynamic result = this.GetOutput(w => w.WritePathParameter(parameter, ""));

                ((bool)result.required).Should().BeTrue();
            }

            [Fact]
            public void ShouldWriteArrays()
            {
                ParameterInfo parameter = CreateParameter("", typeof(int[]));

                dynamic result = this.GetOutput(w => w.WritePathParameter(parameter, ""));

                ((string)result.type).Should().Be("array");
                ((string)result.items.type).Should().Be("integer");
                ((string)result.items.format).Should().Be("int32");
            }

            [Fact]
            public void ShouldWriteTheDescription()
            {
                ParameterInfo parameter = CreateParameter("", typeof(string));

                dynamic result = this.GetOutput(w => w.WritePathParameter(parameter, "Description text"));

                ((string)result.description).Should().Be("Description text");
            }

            [Fact]
            public void ShouldWriteTheName()
            {
                ParameterInfo parameter = CreateParameter("parameterName", typeof(string));

                dynamic result = this.GetOutput(w => w.WritePathParameter(parameter, ""));

                ((string)result.name).Should().Be("parameterName");
            }
        }

        public sealed class WriteQueryParameter : ParameterWriterTests
        {
            [Fact]
            public void ShouldSetAllowEmptyValueForBoolParameters()
            {
                ParameterInfo parameter = CreateParameter("", typeof(bool));

                dynamic result = this.GetOutput(w => w.WriteQueryParameter(parameter, "", ""));

                ((string)result.type).Should().Be("boolean");
                ((bool)result.allowEmptyValue).Should().BeTrue();
            }

            [Fact]
            public void ShouldSetInToQuery()
            {
                ParameterInfo parameter = CreateParameter("", typeof(string));

                dynamic result = this.GetOutput(w => w.WriteQueryParameter(parameter, "", ""));

                ((string)result.@in).Should().Be("query");
            }

            [Fact]
            public void ShouldSetTheDefaultValue()
            {
                ParameterInfo parameter = CreateParameter("", typeof(int));
                parameter.HasDefaultValue.Returns(true);
                parameter.RawDefaultValue.Returns(123);

                dynamic result = this.GetOutput(w => w.WriteQueryParameter(parameter, "", ""));

                ((int)result.@default).Should().Be(123);
            }

            [Fact]
            public void ShouldWriteTheDescription()
            {
                ParameterInfo parameter = CreateParameter("", typeof(string));

                dynamic result = this.GetOutput(w => w.WriteQueryParameter(parameter, "", "Description text"));

                ((string)result.description).Should().Be("Description text");
            }

            [Fact]
            public void ShouldWriteTheQueryKey()
            {
                ParameterInfo parameter = CreateParameter("parameterName", typeof(string));

                dynamic result = this.GetOutput(w => w.WriteQueryParameter(parameter, "queryKey", ""));

                ((string)result.name).Should().Be("queryKey");
            }
        }
    }
}
