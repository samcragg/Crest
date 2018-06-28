namespace Host.UnitTests.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Core;
    using Crest.Host.Engine;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class ConfigurationServiceTests
    {
        private readonly IConfigurationProvider provider;
        private readonly ConfigurationService service;

        public ConfigurationServiceTests()
        {
            this.provider = Substitute.For<IConfigurationProvider>();
            this.service = new ConfigurationService(new[] { this.provider });
        }

        public sealed class CanConfigure : ConfigurationServiceTests
        {
            [Fact]
            public void ShouldReturnFalseIfTheTypeIsNotMarkedAsConfiguration()
            {
                bool result = this.service.CanConfigure(typeof(ConfigurationServiceTests));

                result.Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfTheTypeIsMarkedAsConfiguration()
            {
                bool result = this.service.CanConfigure(typeof(FakeConfiguration));

                result.Should().BeTrue();
            }
        }

        public sealed class InitializeInstance : ConfigurationServiceTests
        {
            [Fact]
            public void ShouldInvokeTheProvidersInOrder()
            {
                IConfigurationProvider provider1 = Substitute.For<IConfigurationProvider>();
                provider1.Order.Returns(1);
                IConfigurationProvider provider2 = Substitute.For<IConfigurationProvider>();
                provider2.Order.Returns(2);
                var service = new ConfigurationService(new[] { provider2, provider1 });

                service.InitializeInstance(new FakeConfiguration(), Substitute.For<IServiceProvider>());

                Received.InOrder(() =>
                {
                    provider1.Inject(Arg.Any<object>());
                    provider2.Inject(Arg.Any<object>());
                });
            }

            [Fact]
            public void ShouldPassTheObjectToTheProviders()
            {
                object instance = new FakeConfiguration();

                this.service.InitializeInstance(instance, Substitute.For<IServiceProvider>());

                this.provider.Received().Inject(instance);
            }
        }

        public sealed class InitializeProvidersAsync : ConfigurationServiceTests
        {
            [Fact]
            public async Task ShouldInitializeTheProviders()
            {
                await this.service.InitializeProvidersAsync(new Type[0]);

                await this.provider.ReceivedWithAnyArgs().InitializeAsync(null);
            }

            [Fact]
            public async Task ShouldPassTheConfigurableClassesToTheProviders()
            {
                Type[] types = new[] { typeof(ConfigurationServiceTests), typeof(FakeConfiguration) };
                await this.provider.InitializeAsync(Arg.Do<IEnumerable<Type>>(t =>
                {
                    t.Should().BeEquivalentTo(typeof(FakeConfiguration));
                }));

                await this.service.InitializeProvidersAsync(types);
            }
        }

        [Configuration]
        private sealed class FakeConfiguration
        {
        }
    }
}
