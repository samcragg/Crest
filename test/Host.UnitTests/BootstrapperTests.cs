namespace Host.UnitTests
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.Engine;
    using FluentAssertions;
    using NSubstitute;
    using NUnit.Framework;

    [TestFixture]
    public class BootstrapperTests
    {
        private FakeBootstrapper bootstrapper;
        private IDiscoveryService discoveryService;
        private IServiceRegister serviceRegister;

        internal interface IFakeInterface
        {
        }

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

        [TestFixture]
        public sealed class Dispose : BootstrapperTests
        {
            [Test]
            public void CanBeCalledMultipleTimes()
            {
                this.bootstrapper.Dispose();

                this.bootstrapper.Invoking(b => b.Dispose()).ShouldNotThrow();
            }

            [Test]
            public void ShouldDisposeOfTheServiceLocator()
            {
                this.bootstrapper.Dispose();

                this.serviceRegister.Received().Dispose();
            }

            [Test]
            public void ShouldSetIsDisposedToTrue()
            {
                this.bootstrapper.IsDisposed.Should().BeFalse();

                this.bootstrapper.Dispose();

                this.bootstrapper.IsDisposed.Should().BeTrue();
            }
        }

        [TestFixture]
        public sealed class Initialize : BootstrapperTests
        {
            private interface IFakeRoute
            {
                Task Route();
            }

            [Test]
            public void ShouldHandleDisposableTypes()
            {
                this.discoveryService.GetDiscoveredTypes().Returns(new[] { typeof(FakeDisposabe) });

                this.bootstrapper.Invoking(b => b.Initialize())
                    .ShouldNotThrow();
            }

            [Test]
            public void ShouldHandleTypesThatCannotBeConstructed()
            {
                this.discoveryService.GetDiscoveredTypes().Returns(new[] { typeof(CannotInject) });

                this.bootstrapper.Invoking(b => b.Initialize())
                    .ShouldNotThrow();
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
            public void ShouldInitializeTheConfigurationService()
            {
                ConfigurationService configurationService = Substitute.For<ConfigurationService>();
                this.serviceRegister.GetConfigurationService()
                    .Returns(configurationService);

                this.bootstrapper.Initialize();

                configurationService.ReceivedWithAnyArgs().InitializeProviders(null);
            }

            [Test]
            public void ShouldRegisterCustomFactories()
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
            public void ShouldRegisterSingletons()
            {
                using (var bootstrapper = new FakeBootstrapper(new ServiceLocator()))
                {
                    this.discoveryService.IsSingleInstance(typeof(IFakeInterface))
                        .Returns(true);

                    this.bootstrapper.Initialize();
                    object instance1 = this.bootstrapper.ServiceLocator.GetService(typeof(IFakeInterface));
                    object instance2 = this.bootstrapper.ServiceLocator.GetService(typeof(IFakeInterface));

                    instance1.Should().BeSameAs(instance2);
                }
            }

            [Test]
            public void ShouldSetTheRouteMapper()
            {
                Assert.That(this.bootstrapper.RouteMapper, Is.Null);

                this.bootstrapper.Initialize();

                this.bootstrapper.RouteMapper.Should().NotBeNull();
            }

            [Test]
            public void ShouldSetTheRouteMetadataFactory()
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

                metadata.Factory.Should().NotBeNull();
            }

            internal class CannotInject : IFakeInterface
            {
                private CannotInject(int arg)
                {
                }
            }

            internal class FakeDisposabe : IDisposable
            {
                internal bool DisposeCalled { get; private set; }

                public void Dispose()
                {
                    this.DisposeCalled = true;
                }
            }
        }

        [TestFixture]
        public sealed class ServiceLocatorProperty : BootstrapperTests
        {
            [Test]
            public void ShouldReturnTheValuePassedToTheConstructor()
            {
                this.bootstrapper.ServiceLocator.Should().BeSameAs(this.serviceRegister);
            }

            [Test]
            public void ShouldThrowIfDisposed()
            {
                this.bootstrapper.Dispose();

                this.bootstrapper.Invoking(b => { var _ = b.ServiceLocator; })
                    .ShouldThrow<ObjectDisposedException>();
            }
        }

        [TestFixture]
        public sealed class ThrowIfDisposed : BootstrapperTests
        {
            [Test]
            public void ShouldIncludeTheDerivedClassName()
            {
                this.bootstrapper.Dispose();

                this.bootstrapper.Invoking(b => b.ThrowIfDisposed())
                    .ShouldThrow<ObjectDisposedException>()
                    .And.ObjectName.Should().Be(nameof(FakeBootstrapper));
            }
        }

        internal class FakeClass : IFakeInterface
        {
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
