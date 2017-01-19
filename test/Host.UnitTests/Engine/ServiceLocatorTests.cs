namespace Host.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.Engine;
    using DryIoc;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class ServiceLocatorTests
    {
        private IContainer container;
        private FakeServiceLocator locator;

        [SetUp]
        public void SetUp()
        {
            this.container = Substitute.For<IContainer>();
            this.locator = new FakeServiceLocator(this.container);
        }

        [Test]
        public void DiposeShouldSetIsDisposedToTrue()
        {
            Assert.That(this.locator.IsDisposed, Is.False);

            this.locator.Dispose();

            Assert.That(this.locator.IsDisposed, Is.True);
        }

        [Test]
        public void DisposeCanBeCalledMultipleTimes()
        {
            this.locator.Dispose();

            Assert.That(() => this.locator.Dispose(), Throws.Nothing);
        }

        [Test]
        public void DisposeShouldDisposeOfTheContainer()
        {
            this.locator.Dispose();

            this.container.Received().Dispose();
        }

        [Test]
        public void ShouldThrowAnExceptionIfMultipleServicesAreRegisteredForASingleItem()
        {
            this.container.Resolve(typeof(IDiscoveryService[]))
                .Returns(new IDiscoveryService[2]);

            Assert.That(
                () => this.locator.GetDiscoveryService(),
                Throws.InstanceOf<InvalidOperationException>()
                      .And.Message.Contains(nameof(IDiscoveryService)));
        }

        [Test]
        public void GetAfterRequestPluginsShouldCheckForDisposed()
        {
            this.locator.Dispose();

            Assert.That(
                () => this.locator.GetAfterRequestPlugins(),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void GetAfterRequestPluginsShouldReturnTheValueFromTheContainer()
        {
            var plugins = new IPostRequestPlugin[0];
            this.container.Resolve(typeof(IPostRequestPlugin[]))
                .Returns(plugins);

            IPostRequestPlugin[] result = this.locator.GetAfterRequestPlugins();

            Assert.That(result, Is.SameAs(plugins));
        }

        [Test]
        public void GetBeforeRequestPluginsShouldCheckForDisposed()
        {
            this.locator.Dispose();

            Assert.That(
                () => this.locator.GetBeforeRequestPlugins(),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void GetBeforeRequestPluginsShouldReturnTheValueFromTheContainer()
        {
            var plugins = new IPreRequestPlugin[0];
            this.container.Resolve(typeof(IPreRequestPlugin[]))
                .Returns(plugins);

            IPreRequestPlugin[] result = this.locator.GetBeforeRequestPlugins();

            Assert.That(result, Is.SameAs(plugins));
        }

        [Test]
        public void GetConfigurationServiceShouldCheckForDisposed()
        {
            this.locator.Dispose();

            Assert.That(
                () => this.locator.GetConfigurationService(),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public async Task GetConfigurationServiceShouldGetTheProvidersFromTheContainer()
        {
            var configurationProvider = Substitute.For<IConfigurationProvider>();
            this.container.Resolve(typeof(IEnumerable<IConfigurationProvider>))
                .Returns(new[] { configurationProvider });

            ConfigurationService result = this.locator.GetConfigurationService();
            await result.InitializeProviders(new Type[0]);

            await configurationProvider.ReceivedWithAnyArgs().Initialize(null);
        }

        [Test]
        public void GetDiscoveryServiceShouldCheckForDisposed()
        {
            this.locator.Dispose();

            Assert.That(
                () => this.locator.GetDiscoveryService(),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void GetDiscoveryServiceShouldGetTheServiceFromTheContainer()
        {
            IDiscoveryService discoveryService = Substitute.For<IDiscoveryService>();
            this.container.Resolve(typeof(IDiscoveryService[]))
                .Returns(new[] { discoveryService });

            IDiscoveryService result = this.locator.GetDiscoveryService();

            Assert.That(result, Is.SameAs(discoveryService));
        }

        [Test]
        public void GetDiscoveryServiceShouldReturnADefaultRegisteredInstance()
        {
            using (var serviceLocator = new ServiceLocator())
            {
                IDiscoveryService result = serviceLocator.GetDiscoveryService();

                Assert.That(result, Is.Not.Null);
            }
        }

        [Test]
        public void GetErrorHandlersShouldCheckForDisposed()
        {
            this.locator.Dispose();

            Assert.That(
                () => this.locator.GetErrorHandlers(),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void GetErrorHandlersShouldReturnTheValueFromTheContainer()
        {
            var plugins = new IErrorHandlerPlugin[0];
            this.container.Resolve(typeof(IErrorHandlerPlugin[]))
                .Returns(plugins);

            IErrorHandlerPlugin[] result = this.locator.GetErrorHandlers();

            Assert.That(result, Is.SameAs(plugins));
        }

        [Test]
        public void GetHtmlTemplateProviderShouldCheckForDisposed()
        {
            this.locator.Dispose();

            Assert.That(
                () => this.locator.GetHtmlTemplateProvider(),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void GetHtmlTemplateProviderShouldReturnTheValueFromTheContainer()
        {
            IHtmlTemplateProvider provider = Substitute.For<IHtmlTemplateProvider>();
            this.container.Resolve(typeof(IHtmlTemplateProvider[]))
                .Returns(new[] { provider });

            IHtmlTemplateProvider result = this.locator.GetHtmlTemplateProvider();

            Assert.That(result, Is.SameAs(provider));
        }

        [Test]
        public void GetHtmlTemplateProviderShouldReturnADefaultRegisteredInstance()
        {
            using (var serviceLocator = new ServiceLocator())
            {
                IHtmlTemplateProvider result = serviceLocator.GetHtmlTemplateProvider();

                Assert.That(result, Is.Not.Null);
            }
        }

        private class FakeServiceLocator : ServiceLocator
        {
            public FakeServiceLocator(IContainer container)
                : base(container)
            {
            }

            internal new bool IsDisposed
            {
                get { return base.IsDisposed; }
            }

            internal new void ThrowIfDisposed()
            {
                base.ThrowIfDisposed();
            }
        }
    }
}
