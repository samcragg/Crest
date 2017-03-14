namespace Host.UnitTests.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Crest.Core;
    using Crest.Host.Engine;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class ConfigurationServiceTests
    {
        private IConfigurationProvider provider;
        private ConfigurationService service;

        [SetUp]
        public void SetUp()
        {
            this.provider = Substitute.For<IConfigurationProvider>();
            this.service = new ConfigurationService(new[] { this.provider });
        }

        [TestFixture]
        public sealed class CanConfigure : ConfigurationServiceTests
        {
            [Test]
            public void ShouldReturnFalseIfTheTypeIsNotMarkedAsConfiguration()
            {
                bool result = this.service.CanConfigure(typeof(ConfigurationServiceTests));

                result.Should().BeFalse();
            }

            [Test]
            public void ShouldReturnTrueIfTheTypeIsMarkedAsConfiguration()
            {
                bool result = this.service.CanConfigure(typeof(FakeConfiguration));

                result.Should().BeTrue();
            }
        }

        [TestFixture]
        public sealed class InitializeInstance : ConfigurationServiceTests
        {
            [Test]
            public void ShouldInvokeTheProvidersInOrder()
            {
                var provider1 = Substitute.For<IConfigurationProvider>();
                provider1.Order.Returns(1);
                var provider2 = Substitute.For<IConfigurationProvider>();
                provider2.Order.Returns(2);
                this.service = new ConfigurationService(new[] { provider2, provider1 });

                this.service.InitializeInstance(new FakeConfiguration(), Substitute.For<IServiceProvider>());

                Received.InOrder(() =>
                {
                    provider1.Inject(Arg.Any<object>());
                    provider2.Inject(Arg.Any<object>());
                });
            }

            [Test]
            public void ShouldPassTheObjectToTheProviders()
            {
                object instance = new FakeConfiguration();

                this.service.InitializeInstance(instance, Substitute.For<IServiceProvider>());

                this.provider.Received().Inject(instance);
            }
        }

        [TestFixture]
        public sealed class InitializeProviders : ConfigurationServiceTests
        {
            [Test]
            public async Task ShouldInitializeTheProviders()
            {
                await this.service.InitializeProviders(new Type[0]);

                await this.provider.ReceivedWithAnyArgs().Initialize(null);
            }

            [Test]
            public async Task ShouldPassTheConfigurableClassesToTheProviders()
            {
                Type[] types = new[] { typeof(ConfigurationServiceTests), typeof(FakeConfiguration) };
                await this.provider.Initialize(Arg.Do<IEnumerable<Type>>(t =>
                {
                    t.Should().BeEquivalentTo(typeof(FakeConfiguration));
                }));

                await this.service.InitializeProviders(types);
            }
        }

        [Configuration]
        private sealed class FakeConfiguration
        {
        }
    }
}
