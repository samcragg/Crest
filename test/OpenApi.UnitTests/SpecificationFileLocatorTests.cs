namespace OpenApi.UnitTests
{
    using Crest.OpenApi;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class SpecificationFileLocatorTests
    {
        private const string Docs = Root + "\\" + SpecificationFileLocator.DocsDirectory;
        private const string Root = @"C:\AssemblyDirectory";
        private readonly IOAdapter io = Substitute.For<IOAdapter>();

        public SpecificationFileLocatorTests()
        {
            this.io.GetBaseDirectory().Returns(Root);
        }

        public sealed class RelativePaths : SpecificationFileLocatorTests
        {
            [Fact]
            public void ShouldReturnAllThePaths()
            {
                this.io.EnumerateFiles(Docs, "openapi.json")
                    .Returns(new[]
                    {
                        Docs + @"\v1\openapi.json",
                        Docs + @"\v2\openapi.json"
                    });

                var locator = new SpecificationFileLocator(this.io);

                locator.RelativePaths.Should().BeEquivalentTo("v1/openapi.json", "v2/openapi.json");
            }

            [Fact]
            public void ShouldReturnGzVersionsIfAvailable()
            {
                this.io.EnumerateFiles(Docs, "openapi.json")
                    .Returns(new[]
                    {
                        Docs + @"\v1\openapi.json",
                        Docs + @"\v2\openapi.json"
                    });
                this.io.EnumerateFiles(Docs, "openapi.json.gz")
                    .Returns(new[]
                    {
                        Docs + @"\v1\openapi.json.gz",
                    });

                var locator = new SpecificationFileLocator(this.io);

                locator.RelativePaths.Should().BeEquivalentTo("v1/openapi.json.gz", "v2/openapi.json");
            }
        }
    }
}
