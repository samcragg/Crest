namespace Host.UnitTests.IO
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Crest.Host.IO;
    using FluentAssertions;
    using Xunit;

    public class FileWriteWatcherTests : IDisposable
    {
        private readonly FileWriteWatcher watcher;

        public FileWriteWatcherTests()
        {
            this.watcher = new FileWriteWatcher();
        }

        public void Dispose()
        {
            this.watcher.Dispose();
        }

        public class StartMonitoring : FileWriteWatcherTests
        {
            [Fact]
            public async Task ShouldListenForFileChanges()
            {
                const string Filename = nameof(FileWriteWatcherTests) + ".json";
                string fullPath = Path.Combine(AppContext.BaseDirectory, Filename);

                try
                {
                    var semaphore = new SemaphoreSlim(0);
                    Func<Task> callback = () =>
                    {
                        semaphore.Release();
                        return Task.CompletedTask;
                    };

                    this.watcher.WatchFile(Filename, callback);
                    this.watcher.StartMonitoring();

                    File.WriteAllText(fullPath, "{}");
                    bool success = await semaphore.WaitAsync(TimeSpan.FromSeconds(5));

                    success.Should().BeTrue();
                }
                finally
                {
                    FileReaderTests.TryDeleteFile(fullPath);
                }
            }
        }

        public class WatchFile : FileWriteWatcherTests
        {
            [Fact]
            public void ShouldThrowIfTheFileIsRegisteredTwice()
            {
                this.watcher.WatchFile("example.txt", null);

                Action action = () => this.watcher.WatchFile("example.txt", null);

                action.Should().Throw<ArgumentException>();
            }
        }
    }
}
