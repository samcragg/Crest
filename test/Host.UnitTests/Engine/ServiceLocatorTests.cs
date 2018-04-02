namespace Host.UnitTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.Engine;
    using DryIoc;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

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

        [Fact]
        public void ShouldThrowAnExceptionIfMultipleServicesAreRegisteredForASingleItem()
        {
            this.container.Resolve(typeof(IDiscoveryService[]))
                .Returns(new IDiscoveryService[2]);

            Action action = () => this.locator.GetDiscoveryService();

            action.Should().Throw<InvalidOperationException>()
                  .WithMessage("*" + nameof(IDiscoveryService) + "*");
        }

        public sealed class CreateScope : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCreateANewScope()
            {
                this.locator.CreateScope();

                this.container.Received().OpenScope();
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
                this.locator.Dispose();

                this.container.Received().Dispose();
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
                this.container.Resolve(typeof(IPostRequestPlugin[]))
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
                this.container.Resolve(typeof(IPreRequestPlugin[]))
                    .Returns(plugins);

                IPreRequestPlugin[] result = this.locator.GetBeforeRequestPlugins();

                result.Should().BeSameAs(plugins);
            }
        }

        public sealed class GetConfigurationService : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCheckForDisposed()
            {
                this.locator.Dispose();

                Action action = () => this.locator.GetConfigurationService();

                action.Should().Throw<ObjectDisposedException>();
            }

            [Fact]
            public async Task ShouldGetTheProvidersFromTheContainer()
            {
                IConfigurationProvider configurationProvider = Substitute.For<IConfigurationProvider>();
                this.container.Resolve(typeof(IEnumerable<IConfigurationProvider>))
                    .Returns(new[] { configurationProvider });

                IConfigurationService result = this.locator.GetConfigurationService();
                await result.InitializeProviders(new Type[0]);

                await configurationProvider.ReceivedWithAnyArgs().Initialize(null);
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
                this.container.Resolve(typeof(IDirectRouteProvider[]))
                    .Returns(providers);

                IDirectRouteProvider[] result = this.locator.GetDirectRouteProviders();

                result.Should().BeSameAs(providers);
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
                this.container.Resolve(typeof(IErrorHandlerPlugin[]))
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
                this.container.Resolve(typeof(string))
                    .Returns("Instance");

                object result = this.locator.GetService(typeof(string));

                result.Should().Be("Instance");
            }
        }

        public sealed class GetSpecificItem : ServiceLocatorTests
        {
            [Theory]
            [InlineData(nameof(ServiceLocator.GetContentConverterFactory))]
            [InlineData(nameof(ServiceLocator.GetDiscoveryService))]
            [InlineData(nameof(ServiceLocator.GetHtmlTemplateProvider))]
            [InlineData(nameof(ServiceLocator.GetResponseStatusGenerator))]
            public void ShouldCheckForDisposed(string methodName)
            {
                MethodInfo method = typeof(ServiceLocator).GetMethod(methodName);
                Action action = () => method.Invoke(this.locator, null);

                this.locator.Dispose();

                action.Should().Throw<TargetInvocationException>()
                      .WithInnerException<ObjectDisposedException>();
            }

            [Theory]
            [InlineData(nameof(ServiceLocator.GetContentConverterFactory))]
            [InlineData(nameof(ServiceLocator.GetDiscoveryService))]
            [InlineData(nameof(ServiceLocator.GetHtmlTemplateProvider))]
            [InlineData(nameof(ServiceLocator.GetResponseStatusGenerator))]
            public void ShouldGetTheServiceFromTheContainer(string methodName)
            {
                MethodInfo method = typeof(ServiceLocator).GetMethod(methodName);

                // The expected type is an array as that's how the TryResolve method
                // determines whether there is a registered instance or not
                Type expectedType = method.ReturnType.MakeArrayType();
                this.container.Resolve(null).ReturnsForAnyArgs(Array.CreateInstance(method.ReturnType, 1));

                method.Invoke(this.locator, null);

                this.container.Received().Resolve(expectedType);
            }

            [Theory]
            [InlineData(nameof(ServiceLocator.GetContentConverterFactory))]
            [InlineData(nameof(ServiceLocator.GetDiscoveryService))]
            [InlineData(nameof(ServiceLocator.GetHtmlTemplateProvider))]
            [InlineData(nameof(ServiceLocator.GetResponseStatusGenerator))]
            public void ShouldReturnADefaultRegisteredInstance(string methodName)
            {
                MethodInfo method = typeof(ServiceLocator).GetMethod(methodName);

                using (var serviceLocator = new ServiceLocator())
                {
                    object result = method.Invoke(serviceLocator, null);

                    result.Should().NotBeNull();
                }
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

                this.container.Received().UseInstance(typeof(ExampleClass), instance);
            }

            [Fact]
            public void ShouldRegisterTheInstanceWithTheScope()
            {
                var instance = new ExampleClass();

                this.locator.UseInstance(typeof(ExampleClass), instance);

                this.scope.Received().UseInstance(typeof(ExampleClass), instance);
            }
        }

        private class ExampleClass : IInterface1, IInterface2
        {
        }

        private class FakeServiceLocator : ServiceLocator
        {
            public FakeServiceLocator(IContainer container, IResolverContext scope)
                : base(container, scope)
            {
            }

            internal new bool IsDisposed => base.IsDisposed;

            internal new void ThrowIfDisposed()
            {
                base.ThrowIfDisposed();
            }
        }
    }
}
