namespace OpenApi.UnitTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Crest.OpenApi;
    using FluentAssertions;
    using Xunit;

    // These are more like integration tests, but the code in the adapter is
    // trivial so should be ok to just smoke test they work
    public class IOAdapterTests
    {
        private readonly IOAdapter adapter = new IOAdapter();

        public sealed class EnumerateFiles : IOAdapterTests
        {
            private const string TestFolder = "8DCB2A7D-E3F1-4A9F-99F8-806BC7E05801";

            [Fact]
            public void ShouldIncludeFilesInSubDirectories()
            {
                string tempPath = Path.Combine(Path.GetTempPath(), TestFolder);
                string rootFolder = tempPath;
                string subFolder = Path.Combine(rootFolder, "sub");

                try
                {
                    DeleteFolder(tempPath);
                    CreateFile(rootFolder, "root.txt");
                    CreateFile(subFolder, "sub.txt");

                    string[] files =
                        this.adapter.EnumerateFiles(rootFolder, "*.txt")
                            .Select(p => Path.GetFileName(p).ToLowerInvariant())
                            .ToArray();

                    files.Should().BeEquivalentTo("root.txt", "sub.txt");
                }
                finally
                {
                    DeleteFolder(tempPath);
                }
            }

            private void CreateFile(string path, string name)
            {
                Directory.CreateDirectory(path);
                File.WriteAllText(Path.Combine(path, name), "Test File");
            }

            private void DeleteFolder(string path)
            {
                try
                {
                    Directory.Delete(path, recursive: true);
                }
                catch
                {
                }
            }
        }

        public sealed class GetBaseDirectory : IOAdapterTests
        {
            [Fact]
            public void ShouldReturnAppContextBaseDirectory()
            {
                string current = AppContext.BaseDirectory;

                string result = this.adapter.GetBaseDirectory();

                result.Should().Be(current);
            }
        }
            

        public sealed class OpenRead : IOAdapterTests
        {
            [Fact]
            public void ShouldOpenTheFileAsAsync()
            {
                // Get a file known to exist...
                string path = typeof(IOAdapterTests).GetTypeInfo().Assembly.Location;

                using (Stream stream = this.adapter.OpenRead(path))
                {
                    stream.Should().BeOfType<FileStream>()
                          .Which.IsAsync.Should().BeTrue();
                }
            }
        }

        public sealed class OpenResource : IOAdapterTests
        {
            [Fact]
            public void ShouldReturnAStreamForTheResource()
            {
                using (Stream result = this.adapter.OpenResource("Crest.OpenApi.SwaggerUI.index.html"))
                {
                    result.Should().NotBeNull();
                }
            }

            [Fact]
            public void ShouldReturnNullForUnknownResources()
            {
                Stream result = this.adapter.OpenResource("unknown");

                result.Should().BeNull();
            }
        }
    }
}
