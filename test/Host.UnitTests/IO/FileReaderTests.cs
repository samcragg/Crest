namespace Host.UnitTests.IO
{
    using System;
    using System.IO;
    using System.Linq;
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
                string filename = Guid.NewGuid().ToString();
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

        public sealed class ReadAllTextAsync : FileReaderTests
        {
            [Fact]
            public async Task ShouldDetectTheEncodingFromTheFile()
            {
                using (var stream = new MemoryStream())
                {
                    byte[] bytes =
                        Encoding.Unicode.GetPreamble()
                            .Concat(Encoding.Unicode.GetBytes("Test"))
                            .ToArray();

                    stream.Write(bytes, 0, bytes.Length);
                    stream.Position = 0;
                    this.reader.SourceStream = stream;

                    string result = await this.reader.ReadAllTextAsync("");

                    result.Should().Be("Test");
                }
            }

            [Fact]
            public void ShouldNotAllowRelativePaths()
            {
                this.reader.Awaiting(r => r.ReadAllTextAsync("../sensative_file"))
                    .Should().Throw<InvalidOperationException>();
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
