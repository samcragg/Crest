namespace Host.UnitTests.IO
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Crest.Host.IO;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class FileReaderTests
    {
        private readonly FakeFileReader reader = new FakeFileReader();

        internal static void TryDeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
            }
        }

        public sealed class ReadAllBytesAsync : FileReaderTests
        {
            [Fact]
            public async Task ShouldReadTheFileAtTheSpecifiedLocation()
            {
                string filename = Path.GetTempFileName();
                try
                {
                    File.WriteAllText(filename, "Test");

                    byte[] contents = await this.reader.ReadAllBytesAsync(filename);

                    Encoding.ASCII.GetString(contents)
                        .Should().Be("Test");
                }
                finally
                {
                    TryDeleteFile(filename);
                }
            }

            [Fact]
            public void ShouldThrowIfTheEndOfStreamIsReachedBeforeAllTheContentIsRead()
            {
                this.reader.SourceStream = Substitute.For<Stream>();
                this.reader.SourceStream.Length.Returns(100);

                Func<Task> action = () => this.reader.ReadAllBytesAsync("");

                action.Should().Throw<EndOfStreamException>();
            }
        }

        private class FakeFileReader : FileReader
        {
            internal Stream SourceStream { get; set; }

            protected override Stream OpenFile(string path)
            {
                if (this.SourceStream == null)
                {
                    return base.OpenFile(path);
                }
                else
                {
                    return this.SourceStream;
                }
            }
        }
    }
}
