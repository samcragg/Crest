namespace Host.UnitTests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Crest.Host;
    using Crest.Host.Engine;
    using DryIoc;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class ServiceLocatorTests
    {
        private readonly IContainer container;
        private readonly FakeServiceLocator locator;

        public ServiceLocatorTests()
        {
            this.container = Substitute.For<IContainer>();
            this.locator = new FakeServiceLocator(this.container);
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

            action.ShouldThrow<InvalidOperationException>()
                  .WithMessage("*" + nameof(IDiscoveryService) + "*");
        }

        public sealed class Dispose : ServiceLocatorTests
        {
            [Fact]
            public void CanBeCalledMultipleTimes()
            {
                this.locator.Dispose();

                Action action = () => this.locator.Dispose();

                action.ShouldNotThrow();
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

                action.ShouldThrow<ObjectDisposedException>();
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

                action.ShouldThrow<ObjectDisposedException>();
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

                action.ShouldThrow<ObjectDisposedException>();
            }

            [Fact]
            public async Task ShouldGetTheProvidersFromTheContainer()
            {
                var configurationProvider = Substitute.For<IConfigurationProvider>();
                this.container.Resolve(typeof(IEnumerable<IConfigurationProvider>))
                    .Returns(new[] { configurationProvider });

                ConfigurationService result = this.locator.GetConfigurationService();
                await result.InitializeProviders(new Type[0]);

                await configurationProvider.ReceivedWithAnyArgs().Initialize(null);
            }
        }

        public sealed class GetErrorHandlers : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCheckForDisposed()
            {
                this.locator.Dispose();

                Action action = () => this.locator.GetErrorHandlers();

                action.ShouldThrow<ObjectDisposedException>();
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

                action.ShouldThrow<ObjectDisposedException>();
            }

            [Fact]
            public void ShouldCheckForNulls()
            {
                Action action = () => this.locator.GetService(null);

                action.ShouldThrow<ArgumentNullException>();
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

                action.ShouldThrow<TargetInvocationException>()
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

                action.ShouldThrow<ObjectDisposedException>();
            }

            [Fact]
            public void ShouldCheckForNulls()
            {
                this.locator.Invoking(l => l.RegisterFactory(null, () => string.Empty))
                    .ShouldThrow<ArgumentNullException>();

                this.locator.Invoking(l => l.RegisterFactory(typeof(string), null))
                    .ShouldThrow<ArgumentNullException>();
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

                action.ShouldThrow<ObjectDisposedException>();
            }

            [Fact]
            public void ShouldCheckForNulls()
            {
                this.locator.Invoking(l => l.RegisterInitializer(null, _ => { }))
                    .ShouldThrow<ArgumentNullException>();

                this.locator.Invoking(l => l.RegisterInitializer(_ => false, null))
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class RegisterMany : ServiceLocatorTests
        {
            [Fact]
            public void ShouldCheckForDisposed()
            {
                this.locator.Dispose();

                Action action = () => this.locator.RegisterMany(Enumerable.Empty<Type>(), _ => false);

                action.ShouldThrow<ObjectDisposedException>();
            }

            [Fact]
            public void ShouldCheckForNulls()
            {
                this.locator.Invoking(l => l.RegisterMany(null, _ => false))
                    .ShouldThrow<ArgumentNullException>();

                this.locator.Invoking(l => l.RegisterMany(Enumerable.Empty<Type>(), null))
                    .ShouldThrow<ArgumentNullException>();
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

        private class ExampleClass : IInterface1, IInterface2
        {
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
