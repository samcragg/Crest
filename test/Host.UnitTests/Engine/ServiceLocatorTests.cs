namespace Host.UnitTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Abstractions;
    using Crest.Host.Engine;
    using DryIoc;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;
    using Arg = NSubstitute.Arg;

    public class ServiceLocatorTests
    {
        private readonly IContainer container;
        private readonly FakeServiceLocator locator;
        private readonly IResolverContext scope;

        public ServiceLocatorTests()
        {
            this.container = Substitute.For<IContainer>();
            this.scope = Substitute.For<IResolverContext>();
            this.locator = new FakeServiceLocator(this.container, this.scope);
        }

        private interface IInterface1
        {
        }

        private interface IInterface2
        {
        }

        public sealed class Create : ServiceLocatorTests
        {
            [Fact]
            public void ShouldBeAbleToCreateTypesThatAreNotRegistered()
            {
                using (var serviceLocator = new ServiceLocator())
                {
                    IDiscoveryService result = serviceLocator.GetDiscoveryService();

                    result.Should().NotBeNull();
                }
            }
        }

        public sealed class CreateScope : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCreateANewScope()
            {
                this.locator.CreateScope();

                this.container.ReceivedWithAnyArgs().WithCurrentScope(null);
            }

            [Fact]
            public void ShouldReturnANewInstance()
            {
                IServiceLocator result = this.locator.CreateScope();

                result.Should().NotBeSameAs(this.locator);
            }
        }

        public sealed class Dispose : ServiceLocatorTests
        {
            [Fact]
            public void CanBeCalledMultipleTimes()
            {
                this.locator.Dispose();

                Action action = () => this.locator.Dispose();

                action.Should().NotThrow();
            }

            [Fact]
            public void ShouldDisposeOfTheContainer()
            {
                var rootService = new ServiceLocator(this.container, null);

                rootService.Dispose();

                this.container.Received().Dispose();
            }

            [Fact]
            public void ShouldDisposeOfTheScope()
            {
                this.locator.Dispose();

                this.scope.Received().Dispose();
                this.container.DidNotReceive().Dispose();
            }

            [Fact]
            public void ShouldSetIsDisposedToTrue()
            {
                this.locator.IsDisposed.Should().BeFalse();

                this.locator.Dispose();

                this.locator.IsDisposed.Should().BeTrue();
            }
        }

        public sealed class GetAfterRequestPlugins : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCheckForDisposed()
            {
                this.locator.Dispose();

                Action action = () => this.locator.GetAfterRequestPlugins();

                action.Should().Throw<ObjectDisposedException>();
            }

            [Fact]
            public void ShouldReturnTheValueFromTheContainer()
            {
                var plugins = new IPostRequestPlugin[0];
                this.container.Resolve(typeof(IPostRequestPlugin[]), Arg.Any<IfUnresolved>())
                    .Returns(plugins);

                IPostRequestPlugin[] result = this.locator.GetAfterRequestPlugins();

                result.Should().BeSameAs(plugins);
            }
        }

        public sealed class GetBeforeRequestPlugins : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCheckForDisposed()
            {
                this.locator.Dispose();

                Action action = () => this.locator.GetBeforeRequestPlugins();

                action.Should().Throw<ObjectDisposedException>();
            }

            [Fact]
            public void ShouldReturnTheValueFromTheContainer()
            {
                var plugins = new IPreRequestPlugin[0];
                this.container.Resolve(typeof(IPreRequestPlugin[]), Arg.Any<IfUnresolved>())
                    .Returns(plugins);

                IPreRequestPlugin[] result = this.locator.GetBeforeRequestPlugins();

                result.Should().BeSameAs(plugins);
            }
        }

        public sealed class GetInitializers : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCheckForDisposed()
            {
                this.locator.Dispose();

                Action action = () => this.locator.GetInitializers();

                action.Should().Throw<ObjectDisposedException>();
            }

            [Fact]
            public void ShouldGetTheProvidersFromTheContainer()
            {
                var initializers = new IStartupInitializer[0];
                this.container.Resolve(typeof(IStartupInitializer[]), Arg.Any<IfUnresolved>())
                    .Returns(initializers);

                IStartupInitializer[] result = this.locator.GetInitializers();

                result.Should().BeSameAs(initializers);
            }
        }

        public sealed class GetDirectRouteProviders : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCheckForDisposed()
            {
                this.locator.Dispose();

                Action action = () => this.locator.GetDirectRouteProviders();

                action.Should().Throw<ObjectDisposedException>();
            }

            [Fact]
            public void ShouldReturnTheValueFromTheContainer()
            {
                var providers = new IDirectRouteProvider[0];
                this.container.Resolve(typeof(IDirectRouteProvider[]), Arg.Any<IfUnresolved>())
                    .Returns(providers);

                IDirectRouteProvider[] result = this.locator.GetDirectRouteProviders();

                result.Should().BeSameAs(providers);
            }
        }

        public sealed class GetDiscoveryService : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCheckForDisposed()
            {
                Action action = () => this.locator.GetDiscoveryService();

                this.locator.Dispose();

                action.Should().Throw<ObjectDisposedException>();
            }

            [Fact]
            public void ShouldGetTheServiceFromTheContainer()
            {
                // TryResolve method determines whether there is a registered
                // instance or not by resolving an array of them
                IDiscoveryService discoveryService = Substitute.For<IDiscoveryService>();
                this.container.Resolve(typeof(IDiscoveryService[]), Arg.Any<IfUnresolved>())
                    .Returns(new[] { discoveryService });

                IDiscoveryService result = this.locator.GetDiscoveryService();

                result.Should().BeSameAs(discoveryService);
            }

            [Fact]
            public void ShouldReturnADefaultRegisteredInstance()
            {
                // An empty array indicates the service does not exist
                this.container.Resolve(typeof(IDiscoveryService[]), Arg.Any<IfUnresolved>())
                    .Returns(new IDiscoveryService[0]);

                this.locator.GetDiscoveryService();

                this.container.Received().Resolve(typeof(DiscoveryService), Arg.Any<IfUnresolved>());
            }

            [Fact]
            public void ShouldThrowAnExceptionIfMultipleServicesAreRegisteredForASingleItem()
            {
                this.container.Resolve(typeof(IDiscoveryService[]), Arg.Any<IfUnresolved>())
                    .Returns(new IDiscoveryService[2]);

                Action action = () => this.locator.GetDiscoveryService();

                action.Should().Throw<InvalidOperationException>()
                      .WithMessage("*" + nameof(IDiscoveryService) + "*");
            }
        }

        public sealed class GetErrorHandlers : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCheckForDisposed()
            {
                this.locator.Dispose();

                Action action = () => this.locator.GetErrorHandlers();

                action.Should().Throw<ObjectDisposedException>();
            }

            [Fact]
            public void ShouldReturnTheValueFromTheContainer()
            {
                var plugins = new IErrorHandlerPlugin[0];
                this.container.Resolve(typeof(IErrorHandlerPlugin[]), Arg.Any<IfUnresolved>())
                    .Returns(plugins);

                IErrorHandlerPlugin[] result = this.locator.GetErrorHandlers();

                result.Should().BeSameAs(plugins);
            }
        }

        public sealed class GetService : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCheckForDisposed()
            {
                this.locator.Dispose();

                Action action = () => this.locator.GetService(typeof(string));

                action.Should().Throw<ObjectDisposedException>();
            }

            [Fact]
            public void ShouldCheckForNulls()
            {
                Action action = () => this.locator.GetService(null);

                action.Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void ShouldReturnAnInstanceOfTheSpecifiedType()
            {
                this.container.Resolve(typeof(string), Arg.Any<IfUnresolved>())
                    .Returns("Instance");

                object result = this.locator.GetService(typeof(string));

                result.Should().Be("Instance");
            }
        }

        public sealed class RegisterFactory : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCheckForDisposed()
            {
                this.locator.Dispose();

                Action action = () => this.locator.RegisterFactory(typeof(string), () => string.Empty);

                action.Should().Throw<ObjectDisposedException>();
            }

            [Fact]
            public void ShouldCheckForNulls()
            {
                this.locator.Invoking(l => l.RegisterFactory(null, () => string.Empty))
                    .Should().Throw<ArgumentNullException>();

                this.locator.Invoking(l => l.RegisterFactory(typeof(string), null))
                    .Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void ShouldUseTheSpecifiedFunctionToCreateTheService()
            {
                string instance = "Returned by factory";

                using (var serviceLocator = new ServiceLocator())
                {
                    serviceLocator.RegisterFactory(typeof(string), () => instance);
                    object result = serviceLocator.GetService(typeof(string));

                    result.Should().BeSameAs(instance);
                }
            }
        }

        public sealed class RegisterInitializer : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCallTheFunctionsOnCreatedInstances()
            {
                using (var serviceLocator = new ServiceLocator())
                {
                    object initializedObject = null;
                    serviceLocator.RegisterInitializer(t => t == typeof(ExampleClass), i => initializedObject = i);

                    // Need to register the type before you can resolve it...
                    serviceLocator.RegisterFactory(typeof(ExampleClass), () => new ExampleClass());
                    object result = serviceLocator.GetService(typeof(ExampleClass));

                    initializedObject.Should().BeSameAs(result);
                }
            }

            [Fact]
            public void ShouldCheckForDisposed()
            {
                this.locator.Dispose();

                Action action = () => this.locator.RegisterInitializer(_ => false, _ => { });

                action.Should().Throw<ObjectDisposedException>();
            }

            [Fact]
            public void ShouldCheckForNulls()
            {
                this.locator.Invoking(l => l.RegisterInitializer(null, _ => { }))
                    .Should().Throw<ArgumentNullException>();

                this.locator.Invoking(l => l.RegisterInitializer(_ => false, null))
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class RegisterMany : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCheckForDisposed()
            {
                this.locator.Dispose();

                Action action = () => this.locator.RegisterMany(Enumerable.Empty<Type>(), _ => false);

                action.Should().Throw<ObjectDisposedException>();
            }

            [Fact]
            public void ShouldCheckForNulls()
            {
                this.locator.Invoking(l => l.RegisterMany(null, _ => false))
                    .Should().Throw<ArgumentNullException>();

                this.locator.Invoking(l => l.RegisterMany(Enumerable.Empty<Type>(), null))
                    .Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void ShouldIgnoreIEnumerableTypes()
            {
                using (var serviceLocator = new ServiceLocator())
                {
                    serviceLocator.RegisterMany(new[] { typeof(FakeEnumerable<>) }, _ => false);

                    // Get all the registered types that extend IEnumerable, hence
                    // the IEnumerable of IEnumerable
                    object result = serviceLocator.GetService(
                        typeof(IEnumerable<IEnumerable<int>>));

                    result.Should().BeAssignableTo<IEnumerable<IEnumerable<int>>>()
                          .Which.Should().BeEmpty();
                }
            }

            [Fact]
            public void ShouldRegisterAllTheImplementingInterfaces()
            {
                using (var serviceLocator = new ServiceLocator())
                {
                    serviceLocator.RegisterMany(new[] { typeof(ExampleClass) }, _ => false);

                    object result1 = serviceLocator.GetService(typeof(IInterface1));
                    object result2 = serviceLocator.GetService(typeof(IInterface2));

                    result1.Should().BeOfType<ExampleClass>();
                    result2.Should().BeOfType<ExampleClass>();
                }
            }

            [Fact]
            public void ShouldRegisterSingleInstanceTypes()
            {
                using (var serviceLocator = new ServiceLocator())
                {
                    serviceLocator.RegisterMany(new[] { typeof(ExampleClass) }, _ => true);

                    object result1 = serviceLocator.GetService(typeof(IInterface1));
                    object result2 = serviceLocator.GetService(typeof(IInterface1));

                    result1.Should().BeSameAs(result2);
                }
            }

            [Fact]
            public void ShouldRegisterTypesAsTransient()
            {
                using (var serviceLocator = new ServiceLocator())
                {
                    serviceLocator.RegisterMany(new[] { typeof(ExampleClass) }, _ => false);

                    object result1 = serviceLocator.GetService(typeof(IInterface1));
                    object result2 = serviceLocator.GetService(typeof(IInterface1));

                    result1.Should().NotBeSameAs(result2);
                }
            }

            public class FakeEnumerable<T> : IEnumerable<T>
            {
                public IEnumerator<T> GetEnumerator()
                {
                    throw new NotImplementedException();
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    throw new NotImplementedException();
                }
            }
        }

        public sealed class UseInstance : ServiceLocatorTests
        {
            [Fact]
            public void ShouldRegisterTheInstanceWithTheContainerIfThereIsNoScope()
            {
                var instance = new ExampleClass();
                var rootContainer = new FakeServiceLocator(this.container, null);

                rootContainer.UseInstance(typeof(ExampleClass), instance);

                ((IResolverContext)this.container).Received().UseInstance(
                    typeof(ExampleClass),
                    instance,
                    Arg.Any<IfAlreadyRegistered>(),
                    Arg.Any<bool>(),
                    Arg.Any<bool>(),
                    Arg.Any<object>());
            }

            [Fact]
            public void ShouldRegisterTheInstanceWithTheScope()
            {
                var instance = new ExampleClass();

                this.locator.UseInstance(typeof(ExampleClass), instance);

                this.scope.Received().UseInstance(
                    typeof(ExampleClass),
                    instance,
                    Arg.Any<IfAlreadyRegistered>(),
                    Arg.Any<bool>(),
                    Arg.Any<bool>(),
                    Arg.Any<object>());
            }
        }

        private class ExampleClass : IInterface1, IInterface2
        {
        }

        private class FakeServiceLocator : ServiceLocator
        {
            private readonly IContainer container;

            public FakeServiceLocator(IContainer container, IResolverContext scope)
                : base(container, scope)
            {
                this.container = container;
            }

            internal new bool IsDisposed => base.IsDisposed;

            internal new void ThrowIfDisposed()
            {
                base.ThrowIfDisposed();
            }

            protected override T Create<T>()
            {
                return (T)this.container.Resolve(typeof(T));
            }
        }
    }
}
