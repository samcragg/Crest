namespace Host.UnitTests
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host;
    using Crest.Host.Diagnostics;
    using Crest.Host.Engine;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class BootstrapperTests
    {
        private readonly FakeBootstrapper bootstrapper;
        private readonly IDiscoveryService discoveryService;
        private readonly IServiceRegister serviceRegister;

        public BootstrapperTests()
        {
            this.discoveryService = Substitute.For<IDiscoveryService>();
            this.discoveryService.GetDiscoveredTypes()
                .Returns(new[] { typeof(IFakeInterface), typeof(FakeClass) });

            this.serviceRegister = Substitute.For<IServiceRegister>();
            this.serviceRegister.GetDiscoveryService().Returns(this.discoveryService);

            this.bootstrapper = new FakeBootstrapper(this.serviceRegister);
        }

        internal interface IFakeInterface
        {
        }

        public sealed class Dispose : BootstrapperTests
        {
            [Fact]
            public void CanBeCalledMultipleTimes()
            {
                this.bootstrapper.Dispose();

                this.bootstrapper.Invoking(b => b.Dispose()).Should().NotThrow();
            }

            [Fact]
            public void ShouldDisposeOfTheServiceLocator()
            {
                this.bootstrapper.Dispose();

                this.serviceRegister.Received().Dispose();
            }

            [Fact]
            public void ShouldSetIsDisposedToTrue()
            {
                this.bootstrapper.IsDisposed.Should().BeFalse();

                this.bootstrapper.Dispose();

                this.bootstrapper.IsDisposed.Should().BeTrue();
            }
        }

        public sealed class Initialize : BootstrapperTests
        {
            private interface IFakeRoute
            {
                Task Route();
            }

            [Fact]
            public void ShouldHandleDisposableTypes()
            {
                this.discoveryService.GetDiscoveredTypes().Returns(new[] { typeof(FakeDisposabe) });

                this.bootstrapper.Invoking(b => b.Initialize())
                    .Should().NotThrow();
            }

            [Fact]
            public void ShouldHandleTypesThatCannotBeConstructed()
            {
                this.discoveryService.GetDiscoveredTypes().Returns(new[] { typeof(CannotInject) });

                this.bootstrapper.Invoking(b => b.Initialize())
                    .Should().NotThrow();
            }

            [Fact]
            public void ShouldInitializeClassesWithTheConfigurationService()
            {
                IConfigurationService configurationService = Substitute.For<IConfigurationService>();
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

            [Fact]
            public void ShouldInitializeTheConfigurationService()
            {
                IConfigurationService configurationService = Substitute.For<IConfigurationService>();
                this.serviceRegister.GetConfigurationService()
                    .Returns(configurationService);

                this.bootstrapper.Initialize();

                configurationService.ReceivedWithAnyArgs().InitializeProviders(null);
            }

            [Fact]
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

            [Fact]
            public void ShouldRegisterDirectRoutes()
            {
                IDirectRouteProvider direct = Substitute.For<IDirectRouteProvider>();
                this.serviceRegister.GetDirectRouteProviders()
                    .Returns(new[] { direct });

                this.bootstrapper.Initialize();

                direct.Received().GetDirectRoutes();
            }

            [Fact]
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

            [Fact]
            public void ShouldSetTheRouteMapper()
            {
                this.bootstrapper.RouteMapper.Should().BeNull();

                this.bootstrapper.Initialize();

                this.bootstrapper.RouteMapper.Should().NotBeNull();
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

        public sealed class ServiceLocatorProperty : BootstrapperTests
        {
            [Fact]
            public void ShouldReturnTheValuePassedToTheConstructor()
            {
                this.bootstrapper.ServiceLocator.Should().BeSameAs(this.serviceRegister);
            }

            [Fact]
            public void ShouldThrowIfDisposed()
            {
                this.bootstrapper.Dispose();

                this.bootstrapper.Invoking(b => _ = b.ServiceLocator)
                    .Should().Throw<ObjectDisposedException>();
            }
        }

        public sealed class ThrowIfDisposed : BootstrapperTests
        {
            [Fact]
            public void ShouldIncludeTheDerivedClassName()
            {
                this.bootstrapper.Dispose();

                this.bootstrapper.Invoking(b => b.ThrowIfDisposed())
                    .Should().Throw<ObjectDisposedException>()
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

            internal new bool IsDisposed => base.IsDisposed;

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
