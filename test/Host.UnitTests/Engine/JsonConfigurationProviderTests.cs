namespace Host.UnitTests.Engine
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Crest.Host.Engine;
    using Crest.Host.IO;
    using Crest.Host.Logging;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    // As we're playing with an environment variable, we can't have the nested
    // tests running in parallel
    [Collection(nameof(JsonConfigurationProviderTests))]
    public class JsonConfigurationProviderTests
    {
        private const string EnvironmentSettingsFile = "appsettings.UnitTests.json";
        private const string GlobalSettingsFile = "appsettings.json";
        private readonly JsonClassGenerator generator;
        private readonly FileReader reader;
        private readonly FileWriteWatcher watcher;
        private JsonConfigurationProvider provider;

        private JsonConfigurationProviderTests()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UnitTests");
            this.generator = Substitute.For<JsonClassGenerator>();
            this.reader = Substitute.For<FileReader>();
            this.watcher = Substitute.For<FileWriteWatcher>();
            this.provider = new JsonConfigurationProvider(this.reader, this.watcher, this.generator);
        }

        public sealed class Dispose : JsonConfigurationProviderTests
        {
            [Fact]
            public void ShouldDisposeTheFileWriteWatcher()
            {
                this.provider.Dispose();

                this.watcher.Received().Dispose();
            }
        }

        public sealed class InitializeAsync : JsonConfigurationProviderTests
        {
            [Fact]
            public async Task ShouldDefaultToProductionIfNoEnvironmentSet()
            {
                Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", string.Empty);
                this.provider = new JsonConfigurationProvider(this.reader, this.watcher, this.generator);

                await this.provider.InitializeAsync(new Type[0]);

                await this.reader.Received().ReadAllBytesAsync("appsettings.Production.json");
            }

            [Fact]
            public async Task ShouldLoadTheEnvironmentSettings()
            {
                await this.provider.InitializeAsync(new Type[0]);

                await this.reader.Received().ReadAllBytesAsync(EnvironmentSettingsFile);
            }

            [Fact]
            public async Task ShouldLoadTheGlobalSettings()
            {
                await this.provider.InitializeAsync(new Type[0]);

                await this.reader.Received().ReadAllBytesAsync(GlobalSettingsFile);
            }

            [Fact]
            public async Task ShouldLogAmbiguousJsonKeys()
            {
                using (FakeLogger.LogInfo logger = FakeLogger.MonitorLogging())
                {
                    this.reader.ReadAllBytesAsync(GlobalSettingsFile)
                        .Returns(Encoding.ASCII.GetBytes(@"{""myConfig"":{}}"));

                    await this.provider.InitializeAsync(new[]
                    {
                        typeof(DuplicateClass1.MyConfig),
                        typeof(DuplicateClass2.MyConfig)
                    });

                    logger.LogLevel.Should().Be(LogLevel.Warn);
                    logger.Message.Should().MatchEquivalentOf("*multiple*");
                }
            }

            [Fact]
            public async Task ShouldLogIOExceptions()
            {
                using (FakeLogger.LogInfo logger = FakeLogger.MonitorLogging())
                {
                    this.reader.ReadAllBytesAsync(null)
                        .ReturnsForAnyArgs<byte[]>(_ => throw new IOException());

                    await this.provider.InitializeAsync(new Type[0]);

                    logger.LogLevel.Should().Be(LogLevel.Error);
                    logger.Message.Should().MatchEquivalentOf("*unable to read*");
                }
            }

            [Fact]
            public async Task ShouldLogJsonExceptions()
            {
                using (FakeLogger.LogInfo logger = FakeLogger.MonitorLogging())
                {
                    this.generator.CreatePopulateMethod(null, null)
                        .ReturnsForAnyArgs(_ => throw new FormatException());

                    await this.provider.InitializeAsync(new[] { typeof(string) });

                    logger.LogLevel.Should().Be(LogLevel.Error);
                    logger.Message.Should().MatchEquivalentOf("*invalid json*");
                }
            }

            [Fact]
            public async Task ShouldLogUnknownJsonKeys()
            {
                using (FakeLogger.LogInfo logger = FakeLogger.MonitorLogging())
                {
                    this.reader.ReadAllBytesAsync(GlobalSettingsFile)
                        .Returns(Encoding.ASCII.GetBytes(@"{""myConfig"":{}}"));

                    await this.provider.InitializeAsync(new Type[0]);

                    logger.LogLevel.Should().Be(LogLevel.Warn);
                    logger.Message.Should().MatchEquivalentOf("*found*");
                }
            }

            private class DuplicateClass1
            {
                public class MyConfig
                {
                }
            }

            private class DuplicateClass2
            {
                public class MyConfig
                {
                }
            }
        }

        public sealed class Inject : JsonConfigurationProviderTests
        {
            [Fact]
            public async Task ShouldApplyGlobalSettings()
            {
                byte[] global = Encoding.ASCII.GetBytes(@"{""myConfig"":null}");
                this.reader.ReadAllBytesAsync(GlobalSettingsFile).Returns(global);

                this.generator.CreatePopulateMethod(null, null)
                    .ReturnsForAnyArgs(x => ((MyConfig)x).Value = "global");

                await this.provider.InitializeAsync(new[] { typeof(MyConfig) });
                var instance = new MyConfig();
                this.provider.Inject(instance);

                instance.Value.Should().Be("global");
            }

            [Fact]
            public void ShouldIgnoreTypesItCantHandle()
            {
                Action action = () => this.provider.Inject(new object());

                action.Should().NotThrow();
            }

            [Fact]
            public async Task ShouldOverwriteGlobalSettingsWithEnvironmentSettings()
            {
                byte[] environemnt = Encoding.ASCII.GetBytes(@"{""myConfig"":""environment""}");
                this.reader.ReadAllBytesAsync(EnvironmentSettingsFile).Returns(environemnt);

                byte[] global = Encoding.ASCII.GetBytes(@"{""myConfig"":""global""}");
                this.reader.ReadAllBytesAsync(GlobalSettingsFile).Returns(global);

                this.generator.CreatePopulateMethod(typeof(MyConfig), Arg.Any<string>())
                    .Returns(ci =>
                    {
                        return x => ((MyConfig)x).Value = ci.Arg<string>();
                    });

                await this.provider.InitializeAsync(new[] { typeof(MyConfig) });
                var instance = new MyConfig();
                this.provider.Inject(instance);

                instance.Value.Should().Be("environment");
            }

            private class MyConfig
            {
                public string Value { get; set; }
            }
        }

        public sealed class Order : JsonConfigurationProviderTests
        {
            [Fact]
            public void ShouldReturnAValueGreaterThanTheDefaultValuePropervider()
            {
                var defaultProvider = new DefaultConfigurationProvider();

                this.provider.Order.Should().BeGreaterThan(defaultProvider.Order);
            }
        }
    }
}
