namespace OpenApi.UnitTests
{
    using System.IO;
    using System.Reflection;
    using Crest.OpenApi;
    using FluentAssertions;
    using Xunit;

    public class IOAdapterTests
    {
        private readonly IOAdapter adapter = new IOAdapter();

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
            public void ShouldReturnNullForUnknownResources()
            {
                Stream result = this.adapter.OpenResource("unknown");

                result.Should().BeNull();
            }

            [Fact]
            public void ShouldReturnAStreamForTheResource()
            {
                using (Stream result = this.adapter.OpenResource("Crest.OpenApi.SwaggerUI.index.html"))
                {
                    result.Should().NotBeNull();
                }
            }
        }
    }
}
