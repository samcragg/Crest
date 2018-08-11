namespace Host.UnitTests.Engine
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Crest.Host.Engine;
    using Crest.Host.IO;
    using Crest.Host.Logging;
    using FluentAssertions;
    using Host.UnitTests.TestHelpers;
    using NSubstitute;
    using Xunit;

    public class JsonConfigurationProviderTests
    {
        private const string EnvironmentSettingsFile = "appsettings.UnitTests.json";
        private const string GlobalSettingsFile = "appsettings.json";
        private readonly JsonClassGenerator generator;
        private readonly JsonConfigurationProvider provider;
        private readonly FileReader reader;
        private readonly FileWriteWatcher watcher;

        private JsonConfigurationProviderTests()
        {
            HostingEnvironment environment = Substitute.For<HostingEnvironment>();
            environment.Name.Returns("UnitTests");

            this.generator = Substitute.For<JsonClassGenerator>();
            this.reader = Substitute.For<FileReader>();
            this.watcher = Substitute.For<FileWriteWatcher>();
            this.provider = new JsonConfigurationProvider(
                this.reader,
                this.watcher,
                this.generator,
                environment);
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
            public async Task ShouldLoadTheEnvironmentSettings()
            {
                await this.provider.InitializeAsync(new Type[0]);

                await this.reader.Received().ReadAllTextAsync(EnvironmentSettingsFile);
            }

            [Fact]
            public async Task ShouldLoadTheGlobalSettings()
            {
                await this.provider.InitializeAsync(new Type[0]);

                await this.reader.Received().ReadAllTextAsync(GlobalSettingsFile);
            }

            [Fact]
            public async Task ShouldLogAmbiguousJsonKeys()
            {
                using (FakeLogger.LogInfo logger = FakeLogger.MonitorLogging())
                {
                    this.reader.ReadAllTextAsync(GlobalSettingsFile)
                        .Returns(@"{""myConfig"":{}}");

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
                    this.reader.ReadAllTextAsync(null)
                        .ReturnsForAnyArgs<string>(_ => throw new IOException());

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
                    this.reader.ReadAllTextAsync(GlobalSettingsFile)
                        .Returns(@"{""myConfig"":{}}");

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
                this.reader.ReadAllTextAsync(GlobalSettingsFile)
                    .Returns(@"{""myConfig"":null}");

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
                this.reader.ReadAllTextAsync(EnvironmentSettingsFile)
                    .Returns(@"{""myConfig"":""environment""}");

                this.reader.ReadAllTextAsync(GlobalSettingsFile)
                    .Returns(@"{""myConfig"":""global""}");

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
