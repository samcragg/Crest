namespace OpenApi.UnitTests
{
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Crest.OpenApi;
    using FluentAssertions;
    using Newtonsoft.Json;
    using NSubstitute;
    using Xunit;

    public class IndexHtmlGeneratorTests
    {
        private const string DocsPrefix = "/" + SpecificationFileLocator.DocsDirectory + "/";
        private const string TemplateResourceName = "Crest.OpenApi.SwaggerUI.index.html";
        private readonly IOAdapter io = Substitute.For<IOAdapter>();
        private readonly SpecificationFileLocator specs = Substitute.For<SpecificationFileLocator>();
        private string htmlTemplate = "{#URLS#}";

        public IndexHtmlGeneratorTests()
        {
            this.io.OpenResource(TemplateResourceName)
                .Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(this.htmlTemplate)));
        }

        public sealed class DefaultTemplate : IndexHtmlGeneratorTests
        {
            [Fact]
            public void AssemblyShouldContainTheResource()
            {
                Assembly assebmly = typeof(OpenApiProvider).GetTypeInfo().Assembly;
                string[] resources = assebmly.GetManifestResourceNames();

                resources.Should().Contain(TemplateResourceName);
            }
        }

        public sealed class GetPage : IndexHtmlGeneratorTests
        {
            [Fact]
            public void ShouldHandleOpenApiJsonGzFiles()
            {
                this.specs.RelativePaths.Returns(new[] { "v1/openapi.json.gz" });

                var generator = new IndexHtmlGenerator(this.io, this.specs);
                dynamic result = this.ReadUrlsJson(generator);

                ((string)result.urls[0].url).Should().Be(DocsPrefix + "v1/openapi.json");
            }

            [Fact]
            public void ShouldReplaceTheUrlsPlaceholder()
            {
                this.specs.RelativePaths.Returns(new[] { "v1/openapi.json" });

                var generator = new IndexHtmlGenerator(this.io, this.specs);
                dynamic result = this.ReadUrlsJson(generator);

                ((string)result.urls[0].url).Should().Be(DocsPrefix + "v1/openapi.json");
                ((string)result.urls[0].name).Should().Be("V1");
            }

            [Fact]
            public void ShouldReturnTheVersionsInOrder()
            {
                this.specs.RelativePaths
                    .Returns(new[]
                    {
                        "v1/openapi.json",
                        "v10/openapi.json",
                        "v2/openapi.json"
                    });

                var generator = new IndexHtmlGenerator(this.io, this.specs);
                dynamic result = this.ReadUrlsJson(generator);

                ((string)result.urls[0].name).Should().Be("V10");
                ((string)result.urls[1].name).Should().Be("V2");
                ((string)result.urls[2].name).Should().Be("V1");
            }

            private dynamic ReadUrlsJson(IndexHtmlGenerator generator)
            {
                using (var reader = new StreamReader(generator.GetPage()))
                {
                    string json = reader.ReadToEnd();
                    return JsonConvert.DeserializeObject(json);
                }
            }
        }
    }
}
