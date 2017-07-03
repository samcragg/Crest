namespace OpenApi.UnitTests
{
    using Crest.OpenApi;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class SpecificationFileLocatorTests
    {
        private readonly IOAdapter io = Substitute.For<IOAdapter>();

        public sealed class Latest : SpecificationFileLocatorTests
        {
            [Fact]
            public void ShouldReturnTheHighestVersion()
            {
                const string Root = @"C:\AssemblyDirectory";
                const string Docs = Root + "\\" + SpecificationFileLocator.DocsDirectory;

                this.io.GetCurrentDirectory().Returns(Root);
                this.io.EnumerateFiles(Docs, "openapi.json")
                    .Returns(new[]
                    {
                        Docs + @"\v1\openapi.json",
                        Docs + @"\v10\openapi.json",
                        Docs + @"\v2\openapi.json"
                    });

                var locator = new SpecificationFileLocator(this.io);

                locator.Latest.Should().Be(SpecificationFileLocator.DocsDirectory + "/v10/openapi.json");
            }
        }

        public sealed class RelativePaths : SpecificationFileLocatorTests
        {
            [Fact]
            public void ShouldReturnAllThePaths()
            {
                const string Root = @"C:\AssemblyDirectory";
                const string Docs = Root + "\\" + SpecificationFileLocator.DocsDirectory;

                this.io.GetCurrentDirectory().Returns(Root);
                this.io.EnumerateFiles(Docs, "openapi.json")
                    .Returns(new[]
                    {
                        Docs + @"\v1\openapi.json",
                        Docs + @"\v2\openapi.json"
                    });

                var locator = new SpecificationFileLocator(this.io);

                locator.RelativePaths.Should().BeEquivalentTo(
                    SpecificationFileLocator.DocsDirectory + "/v1/openapi.json",
                    SpecificationFileLocator.DocsDirectory + "/v2/openapi.json");
            }
        }
    }
}
