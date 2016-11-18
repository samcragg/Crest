namespace Host.UnitTests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.Engine;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public sealed class BootstrapperTests
    {
        private FakeBootstrapper bootstrapper;

        [SetUp]
        public void SetUp()
        {
            this.bootstrapper = new FakeBootstrapper();
            this.bootstrapper.DiscoveryService.GetDiscoveredTypes()
                .Returns(new[] { typeof(IFakeInterface), typeof(FakeClass) });
        }

        [Test]
        public void ServiceProviderCanResolveInternalTypes()
        {
            IServiceProvider provider = this.bootstrapper.OriginalProvider;

            Assert.That(provider.GetService(typeof(IDiscoveryService)), Is.Not.Null);
        }

        [Test]
        public void DiposeShouldSetIsDisposedToTrue()
        {
            Assert.That(this.bootstrapper.IsDisposed, Is.False);

            this.bootstrapper.Dispose();

            Assert.That(this.bootstrapper.IsDisposed, Is.True);
        }

        [Test]
        public void DisposeCanBeCalledMultipleTimes()
        {
            this.bootstrapper.Dispose();

            Assert.That(() => this.bootstrapper.Dispose(), Throws.Nothing);
        }

        [Test]
        public void DisposeShouldDisposeOfResolvedServices()
        {
            this.bootstrapper.DiscoveryService.GetDiscoveredTypes().Returns(new[] { typeof(FakeDisposabe) });
            this.bootstrapper.Initialize();
            var disposable = (FakeDisposabe)this.bootstrapper.OriginalProvider.GetService(typeof(FakeDisposabe));

            this.bootstrapper.Dispose();

            Assert.That(disposable.DisposeCalled, Is.True);
        }

        [Test]
        public void GetAfterRequestPluginsShouldCheckForDisposed()
        {
            this.bootstrapper.Dispose();

            Assert.That(
                () => this.bootstrapper.GetAfterRequestPlugins(),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void GetAfterRequestPluginsShouldReturnTheValueFromTheServiceContainer()
        {
            IPostRequestPlugin[] plugins = new[] { Substitute.For<IPostRequestPlugin>() };
            this.bootstrapper.Provider.GetService(typeof(IPostRequestPlugin[]))
                .Returns(plugins);

            IPostRequestPlugin[] result = this.bootstrapper.GetAfterRequestPlugins();

            Assert.That(result.Single(), Is.SameAs(plugins[0]));
        }

        [Test]
        public void GetBeforeRequestPluginsShouldCheckForDisposed()
        {
            this.bootstrapper.Dispose();

            Assert.That(
                () => this.bootstrapper.GetBeforeRequestPlugins(),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void GetBeforeRequestPluginsShouldReturnTheValueFromTheServiceContainer()
        {
            IPreRequestPlugin[] plugins = new[] { Substitute.For<IPreRequestPlugin>() };
            this.bootstrapper.Provider.GetService(typeof(IPreRequestPlugin[]))
                .Returns(plugins);

            IPreRequestPlugin[] result = this.bootstrapper.GetBeforeRequestPlugins();

            Assert.That(result.Single(), Is.SameAs(plugins[0]));
        }

        [Test]
        public void GetErrorHandlersShouldCheckForDisposed()
        {
            this.bootstrapper.Dispose();

            Assert.That(
                () => this.bootstrapper.GetErrorHandlers(),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void GetErrorHandlersShouldReturnTheValueFromTheServiceContainer()
        {
            IErrorHandlerPlugin[] plugins = new[] { Substitute.For<IErrorHandlerPlugin>() };
            this.bootstrapper.Provider.GetService(typeof(IErrorHandlerPlugin[]))
                .Returns(plugins);

            IErrorHandlerPlugin[] result = this.bootstrapper.GetErrorHandlers();

            Assert.That(result.Single(), Is.SameAs(plugins[0]));
        }

        [Test]
        public void GetServiceShouldCheckForDisposed()
        {
            this.bootstrapper.Dispose();

            Assert.That(
                () => this.bootstrapper.GetService<string>(),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void GetServiceShouldReturnTheValueFromTheServiceContainer()
        {
            this.bootstrapper.Provider.GetService(typeof(string))
                .Returns("String instance");

            string result = this.bootstrapper.GetService<string>();

            Assert.That(result, Is.SameAs("String instance"));
        }

        [Test]
        public void GetDiscoveryServiceShouldGetTheServiceFromTheServiceProvider()
        {
            IDiscoveryService discoveryService = Substitute.For<IDiscoveryService>();
            this.bootstrapper.Provider.GetService(typeof(IDiscoveryService))
                .Returns(discoveryService);

            IDiscoveryService result = this.bootstrapper.OriginalGetDiscoveryService();

            Assert.That(result, Is.SameAs(discoveryService));
        }

        [Test]
        public void InitializeShouldRegisterSingletons()
        {
            this.bootstrapper.DiscoveryService.IsSingleInstance(typeof(IFakeInterface))
                .Returns(true);

            this.bootstrapper.Initialize();
            object instance1 = this.bootstrapper.OriginalProvider.GetService(typeof(IFakeInterface));
            object instance2 = this.bootstrapper.OriginalProvider.GetService(typeof(IFakeInterface));

            Assert.That(instance1, Is.SameAs(instance2));
        }

        [Test]
        public void InitializeShouldRegisterCustomFactories()
        {
            FakeClass fakeInstance = new FakeClass();

            ITypeFactory factory = Substitute.For<ITypeFactory>();
            factory.CanCreate(typeof(IFakeInterface))
                   .Returns(true);
            factory.Create(typeof(IFakeInterface), Arg.Any<IServiceProvider>())
                   .Returns(fakeInstance);

            this.bootstrapper.DiscoveryService.GetCustomFactories()
                .Returns(new[] { factory });

            this.bootstrapper.Initialize();
            object instance = this.bootstrapper.OriginalProvider.GetService(typeof(IFakeInterface));

            Assert.That(instance, Is.SameAs(fakeInstance));
        }

        [Test]
        public void InitializeShouldSetTheRouteMetadataFactory()
        {
            var metadata = new RouteMetadata
            {
                Method = typeof(IFakeRoute).GetMethod(nameof(IFakeRoute.Route)),
                RouteUrl = "route",
                Verb = "GET"
            };

            this.bootstrapper.DiscoveryService.GetDiscoveredTypes().Returns(new[] { typeof(IFakeRoute) });
            this.bootstrapper.DiscoveryService.GetRoutes(typeof(IFakeRoute)).Returns(new[] { metadata });

            this.bootstrapper.Initialize();

            Assert.That(metadata.Factory, Is.Not.Null);
        }

        [Test]
        public void InitializeShouldHandleTypesThatCannotBeConstructed()
        {
            this.bootstrapper.DiscoveryService.GetDiscoveredTypes().Returns(new[] { typeof(CannotInject) });

            Assert.That(
                () => this.bootstrapper.Initialize(),
                Throws.Nothing);
        }

        [Test]
        public void InitializeShouldHandleDisposableTypes()
        {
            this.bootstrapper.DiscoveryService.GetDiscoveredTypes().Returns(new[] { typeof(FakeDisposabe) });

            Assert.That(
                () => this.bootstrapper.Initialize(),
                Throws.Nothing);
        }

        [Test]
        public void ThrowIfDisposedShouldIncludeTheDerivedClassName()
        {
            this.bootstrapper.Dispose();

            Assert.That(
                () => this.bootstrapper.ThrowIfDisposed(),
                Throws.InstanceOf<ObjectDisposedException>()
                      .With.Property(nameof(ObjectDisposedException.ObjectName)).EqualTo(nameof(FakeBootstrapper)));
        }

        internal interface IFakeInterface
        {
        }

        private interface IFakeRoute
        {
            Task Route();
        }

        internal class CannotInject : IFakeInterface
        {
            private CannotInject(int arg)
            {
            }
        }

        internal class FakeClass : IFakeInterface
        {
        }

        internal class FakeDisposabe : IDisposable
        {
            internal bool DisposeCalled { get; private set; }

            public void Dispose()
            {
                this.DisposeCalled = true;
            }
        }

        private class FakeBootstrapper : Bootstrapper
        {
            internal IDiscoveryService DiscoveryService { get; } = Substitute.For<IDiscoveryService>();

            internal IServiceProvider Provider { get; } = Substitute.For<IServiceProvider>();

            internal IServiceProvider OriginalProvider
            {
                get { return base.ServiceProvider; }
            }

            internal new bool IsDisposed
            {
                get { return base.IsDisposed; }
            }

            protected override IServiceProvider ServiceProvider
            {
                get { return this.Provider; }
            }

            internal IDiscoveryService OriginalGetDiscoveryService()
            {
                return base.GetDiscoveryService();
            }

            internal new void Initialize()
            {
                base.Initialize();
            }

            internal new void ThrowIfDisposed()
            {
                base.ThrowIfDisposed();
            }

            protected override IDiscoveryService GetDiscoveryService()
            {
                return this.DiscoveryService;
            }
        }
    }
}
