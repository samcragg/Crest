namespace OpenApi.Generator.UnitTests
{
    using System.Collections;
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading.Tasks;
    using Crest.OpenApi.Generator;
    using FluentAssertions;
    using Newtonsoft.Json;
    using NSubstitute;
    using Xunit;

    public class OpenApiWriterTests
    {
        private const int ApiVersion = 3;
        private const string FakeAssemblyName = "ExampleAssembly";
        private static readonly MethodInfo NoParameterMethod = typeof(FakeMethods).GetMethod(nameof(FakeMethods.NoParemeter));
        private readonly XmlDocParser xmlDoc = Substitute.For<XmlDocParser>();

        private dynamic GetResult(params RouteInformation[] routes)
        {
            // We can't fake out the extension methods so need to use a real one
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(FakeAssemblyName), AssemblyBuilderAccess.Run);

            using (var stringWriter = new StringWriter())
            {
                var openApiWriter = new OpenApiWriter(this.xmlDoc, stringWriter, ApiVersion);
                openApiWriter.WriteHeader(assembly);
                openApiWriter.WriteOperations(routes);
                openApiWriter.WriteFooter();
                return JsonConvert.DeserializeObject(stringWriter.ToString());
            }
        }

        public sealed class WriteFooter : OpenApiWriterTests
        {
            [Fact]
            public void ShouldWriteTheDefinitions()
            {
                dynamic result = this.GetResult();

                ((object)result.definitions).Should().NotBeNull();
            }

            [Fact]
            public void ShouldWriteTheTags()
            {
                dynamic result = this.GetResult();

                ((IEnumerable)result.tags).Should().BeEmpty();
            }
        }

        public sealed class WriteHeader : OpenApiWriterTests
        {
            [Fact]
            public void ShouldWriteTheApiVersion()
            {
                dynamic result = this.GetResult();

                ((string)result.info.version).Should().Be(ApiVersion.ToString());
            }

            [Fact]
            public void ShouldWriteTheInfoSection()
            {
                dynamic result = this.GetResult();

                ((string)result.info.title).Should().Be(FakeAssemblyName);
            }

            [Fact]
            public void ShouldWriteTheSwaggerVersion()
            {
                dynamic result = this.GetResult();

                ((string)result.swagger).Should().Be("2.0");
            }
        }

        public sealed class WriteOperations : OpenApiWriterTests
        {
            [Fact]
            public void ShouldGroupOperationsWithDifferentVerbs()
            {
                dynamic result = this.GetResult(
                    new RouteInformation("get", "/route", NoParameterMethod, ApiVersion, ApiVersion),
                    new RouteInformation("post", "route", NoParameterMethod, ApiVersion, ApiVersion));

                ((object)result.paths["/route"].get).Should().NotBeNull();
                ((object)result.paths["/route"].post).Should().NotBeNull();
            }

            [Fact]
            public void ShouldWriteEmptyPaths()
            {
                // The paths field is required by the spec
                dynamic result = this.GetResult();

                ((object)result.paths).Should().NotBeNull();
            }

            [Fact]
            public void ShouldWritePathsForTheAvailableRoutes()
            {
                dynamic result = this.GetResult(
                    new RouteInformation("get", "too_early", NoParameterMethod, ApiVersion - 2, ApiVersion - 1),
                    new RouteInformation("get", "too_late", NoParameterMethod, ApiVersion + 1, ApiVersion + 2),
                    new RouteInformation("get", "max_route", NoParameterMethod, ApiVersion - 1, ApiVersion),
                    new RouteInformation("get", "min_route", NoParameterMethod, ApiVersion, ApiVersion + 1));

                ((object)result.paths["/too_early"]).Should().BeNull();
                ((object)result.paths["/too_late"]).Should().BeNull();
                ((object)result.paths["/min_route"]).Should().NotBeNull();
                ((object)result.paths["/max_route"]).Should().NotBeNull();
            }
        }

        private class FakeMethods
        {
            public static Task NoParemeter()
            {
                return Task.CompletedTask;
            }
        }
    }
}
