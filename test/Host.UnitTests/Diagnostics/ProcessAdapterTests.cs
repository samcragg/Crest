namespace Host.UnitTests.Diagnostics
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using Xunit;

    // NOTE: These tests rely on the fact that Process caches information
    public class ProcessAdapterTests
    {
        private readonly ProcessAdapter adapter;
        private readonly Process process;

        public ProcessAdapterTests()
        {
            this.process = Process.GetCurrentProcess();
            this.adapter = new ProcessAdapter(this.process);
        }

        public sealed class ApplicationCpuTime : ProcessAdapterTests
        {
            [Fact]
            public void ShouldReturnTheUserProcessorTime()
            {
                this.adapter.ApplicationCpuTime.Should().Be(this.process.UserProcessorTime);
            }
        }

        public sealed class Refresh : ProcessAdapterTests
        {
            [Fact]
            public async Task ShouldClearCachedValues()
            {
                long original = this.adapter.WorkingMemory;
                this.adapter.Refresh();
                await Task.Delay(10);

                // Increase the working memory...
                byte[] array = new byte[20 * 1024 * 1024];
                await Task.Delay(10);

                this.adapter.WorkingMemory.Should().NotBe(original);

                // Prevent the memory from disappearing until we've done the check
                GC.KeepAlive(array);
            }
        }

        public sealed class SystemCpuTime : ProcessAdapterTests
        {
            [Fact]
            public void ShouldReturnThePrivilegedProcessorTime()
            {
                this.adapter.SystemCpuTime.Should().Be(this.process.PrivilegedProcessorTime);
            }
        }

        public sealed class UpTime : ProcessAdapterTests
        {
            [Fact]
            public void ShouldReturnTheAmountOfTimeSinceTheProcessStarted()
            {
                // Wait a little if the process has just been created to pass the
                // tolerance amount
                Thread.Sleep(32);
                TimeSpan amount = DateTime.UtcNow - this.process.StartTime.ToUniversalTime();

                this.adapter.UpTime.Should().BeCloseTo(amount, 16);
            }
        }

        public sealed class VirtualMemory : ProcessAdapterTests
        {
            [Fact]
            public void ShouldReturnTheVirtualMemorySize64()
            {
                this.adapter.VirtualMemory.Should().Be(this.process.VirtualMemorySize64);
            }
        }

        public sealed class WorkingMemory : ProcessAdapterTests
        {
            [Fact]
            public void ShouldReturnTheWorkingSet()
            {
                this.adapter.WorkingMemory.Should().Be(this.process.WorkingSet64);
            }
        }
    }
}
