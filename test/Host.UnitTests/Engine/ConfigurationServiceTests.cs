namespace Host.UnitTests.Engine
{
    using System;
    using System.Threading.Tasks;
    using Crest.Core;
    using Crest.Host.Engine;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ConfigurationServiceTests
    {
        private IConfigurationProvider provider;
        private ConfigurationService service;

        [SetUp]
        public void SetUp()
        {
            this.provider = Substitute.For<IConfigurationProvider>();
            this.service = new ConfigurationService(new[] { this.provider });
        }

        [Test]
        public void CanConfigureShouldReturnTrueIfTheTypeIsMarkedAsConfiguration()
        {
            Assert.That(this.service.CanConfigure(typeof(FakeConfiguration)), Is.True);
        }

        [Test]
        public void CanConfigureShouldReturnFalseIfTheTypeIsNotMarkedAsConfiguration()
        {
            Assert.That(this.service.CanConfigure(typeof(ConfigurationServiceTests)), Is.False);
        }

        [Test]
        public void InitializeInstanceShouldPassTheObjectToTheProviders()
        {
            object instance = new FakeConfiguration();

            this.service.InitializeInstance(instance, Substitute.For<IServiceProvider>());

            this.provider.Received().Inject(instance);
        }

        [Test]
        public void InitializeInstanceShouldInvokeTheProvidersInOrder()
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
        public async Task InitializeProvidersShouldInitializeThePRoviders()
        {
            await this.service.InitializeProviders();

            await this.provider.Received().Initialize();
        }

        [Configuration]
        private sealed class FakeConfiguration
        {
        }
    }
}
