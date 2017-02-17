namespace Host.UnitTests
{
    using System;
    using System.Collections.Generic;
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
        private IDiscoveryService discoveryService;
        private IServiceRegister serviceRegister;

        [SetUp]
        public void SetUp()
        {
            this.discoveryService = Substitute.For<IDiscoveryService>();
            this.discoveryService.GetDiscoveredTypes()
                .Returns(new[] { typeof(IFakeInterface), typeof(FakeClass) });

            this.serviceRegister = Substitute.For<IServiceRegister>();
            this.serviceRegister.GetDiscoveryService().Returns(this.discoveryService);

            this.bootstrapper = new FakeBootstrapper(this.serviceRegister);
        }

        [Test]
        public void ServiceLocatorShouldThrowIfDisposed()
        {
            this.bootstrapper.Dispose();

            Assert.That(
                () => this.bootstrapper.ServiceLocator,
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void ServiceLocatorShouldReturnTheValuePassedToTheConstructor()
        {
            Assert.That(this.bootstrapper.ServiceLocator, Is.SameAs(this.serviceRegister));
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
        public void DisposeShouldDisposeOfTheServiceLocator()
        {
            this.bootstrapper.Dispose();

            this.serviceRegister.Received().Dispose();
        }

        [Test]
        public void InitializeShouldRegisterSingletons()
        {
            using (var bootstrapper = new FakeBootstrapper(new ServiceLocator()))
            {
                this.discoveryService.IsSingleInstance(typeof(IFakeInterface))
                    .Returns(true);

                this.bootstrapper.Initialize();
                object instance1 = this.bootstrapper.ServiceLocator.GetService(typeof(IFakeInterface));
                object instance2 = this.bootstrapper.ServiceLocator.GetService(typeof(IFakeInterface));

                Assert.That(instance1, Is.SameAs(instance2));
            }
        }

        [Test]
        public void InitializeShouldRegisterCustomFactories()
        {
            ITypeFactory factory = Substitute.For<ITypeFactory>();
            factory.CanCreate(typeof(IFakeInterface))
                   .Returns(true);

            this.discoveryService.GetCustomFactories()
                .Returns(new[] { factory });

            // Force the lambdas to get called
            this.serviceRegister.RegisterFactory(
                typeof(IFakeInterface),
                Arg.Do<Func<object>>(x => x()));

            this.bootstrapper.Initialize();

            factory.Received().Create(typeof(IFakeInterface), this.serviceRegister);
        }

        [Test]
        public void InitializeShouldSetTheRouteMapper()
        {
            Assert.That(this.bootstrapper.RouteMapper, Is.Null);

            this.bootstrapper.Initialize();

            Assert.That(this.bootstrapper.RouteMapper, Is.Not.Null);
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

            this.discoveryService.GetDiscoveredTypes().Returns(new[] { typeof(IFakeRoute) });
            this.discoveryService.GetRoutes(typeof(IFakeRoute)).Returns(new[] { metadata });

            this.bootstrapper.Initialize();

            Assert.That(metadata.Factory, Is.Not.Null);
        }

        [Test]
        public void InitializeShouldHandleTypesThatCannotBeConstructed()
        {
            this.discoveryService.GetDiscoveredTypes().Returns(new[] { typeof(CannotInject) });

            Assert.That(
                () => this.bootstrapper.Initialize(),
                Throws.Nothing);
        }

        [Test]
        public void InitializeShouldHandleDisposableTypes()
        {
            this.discoveryService.GetDiscoveredTypes().Returns(new[] { typeof(FakeDisposabe) });

            Assert.That(
                () => this.bootstrapper.Initialize(),
                Throws.Nothing);
        }

        [Test]
        public void InitializeShouldInitializeTheConfigurationService()
        {
            ConfigurationService configurationService = Substitute.For<ConfigurationService>();
            this.serviceRegister.GetConfigurationService()
                .Returns(configurationService);

            this.bootstrapper.Initialize();

            configurationService.ReceivedWithAnyArgs().InitializeProviders(null);
        }

        [Test]
        public void ShouldInitializeClassesWithTheConfigurationService()
        {
            ConfigurationService configurationService = Substitute.For<ConfigurationService>();
            this.serviceRegister.GetConfigurationService()
                .Returns(configurationService);

            // Force the passed in lambdas to be invoked
            object toInitialize = new FakeClass();
            this.serviceRegister.RegisterInitializer(
                Arg.Do<Func<Type, bool>>(x => x(typeof(FakeClass))),
                Arg.Do<Action<object>>(x => x(toInitialize)));

            this.bootstrapper.Initialize();

            configurationService.Received().CanConfigure(typeof(FakeClass));
            configurationService.Received().InitializeInstance(toInitialize, this.serviceRegister);
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
            internal FakeBootstrapper(IServiceRegister register)
                : base(register)
            {
            }

            internal new bool IsDisposed
            {
                get { return base.IsDisposed; }
            }

            internal new void Initialize()
            {
                base.Initialize();
            }

            internal new void ThrowIfDisposed()
            {
                base.ThrowIfDisposed();
            }
        }
    }
}
