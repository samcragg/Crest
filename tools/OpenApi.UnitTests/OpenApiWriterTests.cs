namespace OpenApi.UnitTests
{
    using System.IO;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading.Tasks;
    using Crest.OpenApi;
    using Newtonsoft.Json;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class OpenApiWriterTests
    {
        private const string FakeAssemblyName = "ExampleAssembly";
        private const int ApiVersion = 3;
        private static readonly MethodInfo NoParameterMethod = typeof(FakeMethods).GetMethod(nameof(FakeMethods.NoParemeter));
        private XmlDocParser xmlDoc;

        [SetUp]
        public void SetUp()
        {
            this.xmlDoc = Substitute.For<XmlDocParser>();
        }

        [Test]
        public void WriteHeaderShouldWriteTheSwaggerVersion()
        {
            dynamic result = this.GetResult();

            Assert.That((string)result.swagger, Is.EqualTo("2.0"));
        }

        [Test]
        public void WriteHeaderShouldWriteTheInfoSection()
        {
            dynamic result = this.GetResult();

            Assert.That((string)result.info.title, Is.EqualTo(FakeAssemblyName));
        }

        [Test]
        public void WriteHeaderShouldWriteTheApiVersion()
        {
            dynamic result = this.GetResult();

            Assert.That((string)result.info.version, Is.EqualTo(ApiVersion.ToString()));
        }

        [Test]
        public void WriteOperationsShouldWriteEmptyPaths()
        {
            // The paths field is required by the spec
            dynamic result = this.GetResult();

            Assert.That(result.paths, Is.Not.Null);
        }

        [Test]
        public void WriteOperationsShouldWritePathsForTheAvailableRoutes()
        {
            dynamic result = this.GetResult(
                new RouteInformation("get", "too_early", NoParameterMethod, ApiVersion - 2, ApiVersion - 1),
                new RouteInformation("get", "too_late", NoParameterMethod, ApiVersion + 1, ApiVersion + 2),
                new RouteInformation("get", "max_route", NoParameterMethod, ApiVersion - 1, ApiVersion),
                new RouteInformation("get", "min_route", NoParameterMethod, ApiVersion, ApiVersion + 1));

            Assert.That(result.paths["/too_early"], Is.Null);
            Assert.That(result.paths["/too_late"], Is.Null);
            Assert.That(result.paths["/min_route"], Is.Not.Null);
            Assert.That(result.paths["/max_route"], Is.Not.Null);
        }

        [Test]
        public void WriteOperationsShouldGroupOperationsWithDifferentVerbs()
        {
            dynamic result = this.GetResult(
                new RouteInformation("get", "/route", NoParameterMethod, ApiVersion, ApiVersion),
                new RouteInformation("post", "route", NoParameterMethod, ApiVersion, ApiVersion));

            Assert.That(result.paths["/route"].get, Is.Not.Null);
            Assert.That(result.paths["/route"].post, Is.Not.Null);
        }

        [Test]
        public void WriteFooterShouldWriteTheDefinitions()
        {
            dynamic result = this.GetResult();

            Assert.That(result.definitions, Is.Not.Null);
        }

        [Test]
        public void WriteFooterShouldWriteTheTags()
        {
            dynamic result = this.GetResult();

            Assert.That(result.tags, Has.Count.EqualTo(0));
        }

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

        private class FakeMethods
        {
            public static Task NoParemeter()
            {
                return Task.CompletedTask;
            }
        }
    }
}
