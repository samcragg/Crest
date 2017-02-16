namespace Host.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
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
        public void GetServiceShouldCheckForNulls()
        {
            Assert.That(
                () => this.locator.GetService(null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void GetServiceShouldCheckForDisposed()
        {
            this.locator.Dispose();

            Assert.That(
                () => this.locator.GetService(typeof(string)),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void GetServiceShouldReturnAnInstanceOfTheSpecifiedType()
        {
            this.container.Resolve(typeof(string))
                .Returns("Instance");

            object result = this.locator.GetService(typeof(string));

            Assert.That(result, Is.EqualTo("Instance"));
        }

        [TestCase(nameof(ServiceLocator.GetContentConverterFactory))]
        [TestCase(nameof(ServiceLocator.GetDiscoveryService))]
        [TestCase(nameof(ServiceLocator.GetHtmlTemplateProvider))]
        [TestCase(nameof(ServiceLocator.GetResponseStatusGenerator))]
        public void GetSpecificItemShouldCheckForDisposed(string methodName)
        {
            MethodInfo method = typeof(ServiceLocator).GetMethod(methodName);

            this.locator.Dispose();

            // Throws TargetInvocationException, so check the inner exception
            Assert.That(
                () => method.Invoke(this.locator, null),
                Throws.InnerException.InstanceOf<ObjectDisposedException>());
        }

        [TestCase(nameof(ServiceLocator.GetContentConverterFactory))]
        [TestCase(nameof(ServiceLocator.GetDiscoveryService))]
        [TestCase(nameof(ServiceLocator.GetHtmlTemplateProvider))]
        [TestCase(nameof(ServiceLocator.GetResponseStatusGenerator))]
        public void GetSpecificItemShouldGetTheServiceFromTheContainer(string methodName)
        {
            MethodInfo method = typeof(ServiceLocator).GetMethod(methodName);

            // The expected type is an array as that's how the TryResolve method
            // determines whether there is a registered instance or not
            Type expectedType = method.ReturnType.MakeArrayType();
            this.container.Resolve(null).ReturnsForAnyArgs(Array.CreateInstance(method.ReturnType, 1));

            method.Invoke(this.locator, null);

            this.container.Received().Resolve(expectedType);
        }

        [TestCase(nameof(ServiceLocator.GetContentConverterFactory))]
        [TestCase(nameof(ServiceLocator.GetDiscoveryService))]
        [TestCase(nameof(ServiceLocator.GetHtmlTemplateProvider))]
        [TestCase(nameof(ServiceLocator.GetResponseStatusGenerator))]
        public void GetSpecificItemShouldReturnADefaultRegisteredInstance(string methodName)
        {
            MethodInfo method = typeof(ServiceLocator).GetMethod(methodName);

            using (var serviceLocator = new ServiceLocator())
            {
                object result = method.Invoke(serviceLocator, null);

                Assert.That(result, Is.Not.Null);
            }
        }

        [Test]
        public void RegisterFactoryShouldCheckForNulls()
        {
            Assert.That(
                () => this.locator.RegisterFactory(null, () => string.Empty),
                Throws.InstanceOf<ArgumentNullException>());

            Assert.That(
                () => this.locator.RegisterFactory(typeof(string), null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void RegisterFactoryShouldCheckForDisposed()
        {
            this.locator.Dispose();

            Assert.That(
                () => this.locator.RegisterFactory(typeof(string), () => string.Empty),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void RegisterFactoryShouldUseTheSpecifiedFunctionToCreateTheService()
        {
            string instance = "Returned by factory";

            using (var serviceLocator = new ServiceLocator())
            {
                serviceLocator.RegisterFactory(typeof(string), () => instance);
                object result = serviceLocator.GetService(typeof(string));

                Assert.That(result, Is.SameAs(instance));
            }
        }

        [Test]
        public void RegisterInitializerShouldCheckForNulls()
        {
            Assert.That(
                () => this.locator.RegisterInitializer(null, _ => { }),
                Throws.InstanceOf<ArgumentNullException>());

            Assert.That(
                () => this.locator.RegisterInitializer(_ => false, null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void RegisterInitializerShouldCheckForDisposed()
        {
            this.locator.Dispose();

            Assert.That(
                () => this.locator.RegisterInitializer(_ => false, _ => { }),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void RegisterInitializerShouldCallTheFunctionsOnCreatedInstances()
        {
            using (var serviceLocator = new ServiceLocator())
            {
                object instance = null;
                serviceLocator.RegisterInitializer(t => t == typeof(ExampleClass), i => instance = i);

                // Need to register the type before you can resolve it...
                serviceLocator.RegisterFactory(typeof(ExampleClass), () => new ExampleClass());
                object result = serviceLocator.GetService(typeof(ExampleClass));

                Assert.That(instance, Is.SameAs(result));
            }
        }

        [Test]
        public void RegisterManyShouldCheckForNulls()
        {
            Assert.That(
                () => this.locator.RegisterMany(null, _ => false),
                Throws.InstanceOf<ArgumentNullException>());

            Assert.That(
                () => this.locator.RegisterMany(Enumerable.Empty<Type>(), null),
                Throws.InstanceOf<ArgumentNullException>());
        }

        [Test]
        public void RegisterManyShouldCheckForDisposed()
        {
            this.locator.Dispose();

            Assert.That(
                () => this.locator.RegisterMany(Enumerable.Empty<Type>(), _ => false),
                Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void RegisterManyShouldRegisterTypesAsTransient()
        {
            using (var serviceLocator = new ServiceLocator())
            {
                serviceLocator.RegisterMany(new[] { typeof(ExampleClass) }, _ => false);

                object result1 = serviceLocator.GetService(typeof(IInterface1));
                object result2 = serviceLocator.GetService(typeof(IInterface1));

                Assert.That(result1, Is.Not.SameAs(result2));
            }
        }

        [Test]
        public void RegisterManyShouldRegisterAllTheImplementingInterfaces()
        {
            using (var serviceLocator = new ServiceLocator())
            {
                serviceLocator.RegisterMany(new[] { typeof(ExampleClass) }, _ => false);

                object result1 = serviceLocator.GetService(typeof(IInterface1));
                object result2 = serviceLocator.GetService(typeof(IInterface2));

                Assert.That(result1, Is.InstanceOf<ExampleClass>());
                Assert.That(result2, Is.InstanceOf<ExampleClass>());
            }
        }

        [Test]
        public void RegisterManyShouldRegisterSingleInstanceTypes()
        {
            using (var serviceLocator = new ServiceLocator())
            {
                serviceLocator.RegisterMany(new[] { typeof(ExampleClass) }, _ => true);

                object result1 = serviceLocator.GetService(typeof(IInterface1));
                object result2 = serviceLocator.GetService(typeof(IInterface1));

                Assert.That(result1, Is.SameAs(result2));
            }
        }

        private interface IInterface1
        {
        }

        private interface IInterface2
        {
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
