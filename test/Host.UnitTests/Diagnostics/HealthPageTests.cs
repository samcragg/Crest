namespace Host.UnitTests.Diagnostics
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class HealthPageTests
    {
        private readonly ExecutingAssembly assemblyInfo = Substitute.For<ExecutingAssembly>();
        private readonly HealthPage health;
        private readonly Metrics metrics = Substitute.For<Metrics>();
        private readonly ProcessAdapter process = Substitute.For<ProcessAdapter>();
        private readonly IHtmlTemplateProvider template = Substitute.For<IHtmlTemplateProvider>();
        private readonly ITimeProvider time = Substitute.For<ITimeProvider>();

        public HealthPageTests()
        {
            this.health = new HealthPage(
                this.template,
                this.time,
                this.process,
                this.assemblyInfo,
                this.metrics);
        }

        private async Task<string> GetHtml()
        {
            using (var stream = new MemoryStream())
            {
                await this.health.WriteToAsync(stream);

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public sealed class WriteToAsync : HealthPageTests
        {
            [Fact]
            public async Task ShouldOutputTheCpuUsage()
            {
                this.process.ApplicationCpuTime.Returns(TimeSpan.FromMinutes(1.2));
                this.process.SystemCpuTime.Returns(TimeSpan.FromMinutes(2.4));

                string html = await this.GetHtml();

                html.Should().Contain("01:12")
                    .And.Contain("02:24");
            }

            [Fact]
            public async Task ShouldOutputTheCurrentTime()
            {
                this.time.GetUtc().Returns(new DateTime(2013, 12, 11, 14, 15, 16, DateTimeKind.Utc));

                string html = await this.GetHtml();

                html.Should().Contain("2013-12-11 14:15:16Z");
            }

            [Fact]
            public async Task ShouldOutputTheLoadedAssemblies()
            {
                this.assemblyInfo.GetCompileLibraries()
                    .Returns(new[] { new ExecutingAssembly.AssemblyInfo("Test.Assembly", "1.2.3") });

                string html = await this.GetHtml();

                html.Should().Contain("Test.Assembly")
                    .And.Contain("1.2.3");
            }

            [Fact]
            public async Task ShouldOutputTheMachineName()
            {
                string html = await this.GetHtml();

                html.Should().Contain(Environment.MachineName);
            }

            [Fact]
            public async Task ShouldOutputTheMemoryUsage()
            {
                this.process.PrivateMemory.Returns(1024);
                this.process.WorkingMemory.Returns(2048);

                string html = await this.GetHtml();

                html.Should().Contain("1.00 KiB")
                    .And.Contain("2.00 KiB");
            }

            [Fact]
            public async Task ShouldOutputTheMetrics()
            {
                this.metrics.When(m => m.WriteTo(Arg.Any<IReporter>()))
                    .Do(ci => ci.Arg<IReporter>().Write("Counter", new Counter(), null));

                string html = await this.GetHtml();

                html.Should().Contain("Metrics")
                    .And.Contain("Counter");
            }

            [Fact]
            public async Task ShouldOutputTheProcessUptime()
            {
                this.process.UpTime.Returns(TimeSpan.FromMinutes(1.2));

                string html = await this.GetHtml();

                html.Should().Contain("01:12");
            }

            [Fact]
            public async Task ShouldOutputTheTemplate()
            {
                this.template.Template.Returns("BEFORE" + "AFTER");
                this.template.ContentLocation.Returns("BEFORE".Length);

                string html = await this.GetHtml();

                html.Should().StartWith("BEFORE")
                    .And.EndWith("AFTER");
            }
        }
    }
}
