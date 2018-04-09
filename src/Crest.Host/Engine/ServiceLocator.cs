// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Crest.Abstractions;
    using DryIoc;

    /// <summary>
    /// Creates the instances of interfaces required during initialization.
    /// </summary>
    public class ServiceLocator : IServiceRegister, IServiceLocator, IDisposable
    {
        private readonly IContainer container;
        private readonly IResolverContext scope;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceLocator"/> class.
        /// </summary>
        public ServiceLocator()
            : this(new Container(), null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceLocator"/> class.
        /// </summary>
        /// <param name="container">The container to use.</param>
        /// <param name="scope">The current scope.</param>
        /// <remarks>Internal to allow unit testing.</remarks>
        internal ServiceLocator(IContainer container, IResolverContext scope)
        {
            this.container = container;
            this.scope = scope;
            this.scope?.UseInstance<IScopedServiceRegister>(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ServiceLocator"/> class.
        /// </summary>
        ~ServiceLocator()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets a value indicating whether the object has been disposed or not.
        /// </summary>
        protected bool IsDisposed
        {
            get;
            private set;
        }

        /// <inheritdoc />
        public IServiceLocator CreateScope()
        {
            return new ServiceLocator(this.container, this.container.OpenScope());
        }

        /// <summary>
        /// Releases all resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public IPostRequestPlugin[] GetAfterRequestPlugins()
        {
            this.ThrowIfDisposed();

            return this.Resolve<IPostRequestPlugin[]>();
        }

        /// <inheritdoc />
        public IPreRequestPlugin[] GetBeforeRequestPlugins()
        {
            this.ThrowIfDisposed();

            return this.Resolve<IPreRequestPlugin[]>();
        }

        /// <inheritdoc />
        public IConfigurationService GetConfigurationService()
        {
            this.ThrowIfDisposed();

            IEnumerable<IConfigurationProvider> providers =
                this.Resolve<IEnumerable<IConfigurationProvider>>();

            return new ConfigurationService(providers);
        }

        /// <inheritdoc />
        public IDirectRouteProvider[] GetDirectRouteProviders()
        {
            this.ThrowIfDisposed();

            return this.Resolve<IDirectRouteProvider[]>();
        }

        /// <inheritdoc />
        public IDiscoveryService GetDiscoveryService()
        {
            this.ThrowIfDisposed();

            return this.TryResolve<IDiscoveryService, DiscoveryService>();
        }

        /// <inheritdoc />
        public IErrorHandlerPlugin[] GetErrorHandlers()
        {
            this.ThrowIfDisposed();

            return this.Resolve<IErrorHandlerPlugin[]>();
        }

        /// <inheritdoc />
        public object GetService(Type serviceType)
        {
            Check.IsNotNull(serviceType, nameof(serviceType));
            this.ThrowIfDisposed();

            return this.Resolve(serviceType);
        }

        /// <inheritdoc />
        public IServiceRegister GetServiceRegister()
        {
            return this;
        }

        /// <inheritdoc />
        public void RegisterFactory(Type serviceType, Func<object> factory)
        {
            Check.IsNotNull(serviceType, nameof(serviceType));
            Check.IsNotNull(factory, nameof(factory));
            this.ThrowIfDisposed();

            this.container.RegisterDelegate(
                serviceType,
                _ => factory(),
                ifAlreadyRegistered: IfAlreadyRegistered.Replace);
        }

        /// <inheritdoc />
        public void RegisterInitializer(Func<Type, bool> condition, Action<object> initialize)
        {
            Check.IsNotNull(initialize, nameof(initialize));
            Check.IsNotNull(condition, nameof(condition));
            this.ThrowIfDisposed();

            // The ImplementationType will be null if we're using a factory
            // to create the type, hence the fall-back to service type.
            this.container.RegisterInitializer<object>(
                (instance, _) => initialize(instance),
                ri => condition(ri.ImplementationType ?? ri.ServiceType));
        }

        /// <inheritdoc />
        public void RegisterMany(IEnumerable<Type> types, Func<Type, bool> isSingleInstance)
        {
            Check.IsNotNull(types, nameof(types));
            Check.IsNotNull(isSingleInstance, nameof(isSingleInstance));
            this.ThrowIfDisposed();

            foreach (Type type in types.Where(IsImplementationType))
            {
                Type[] serviceTypes = type.GetImplementedServiceTypes(nonPublicServiceTypes: true);
                this.RegisterMany(serviceTypes, type, isSingleInstance(type));
            }
        }

        /// <inheritdoc />
        public void UseInstance(Type serviceType, object instance)
        {
            (this.scope ?? this.container).UseInstance(serviceType, instance);
        }

        /// <summary>
        /// Returns an instance of the specified concrete type whilst ensuring
        /// any dependencies are injected.
        /// </summary>
        /// <typeparam name="T">The type to create.</typeparam>
        /// <returns>An instance of the specified class.</returns>
        protected virtual T Create<T>()
            where T : class
        {
            return this.container.New<T>();
        }

        /// <summary>
        /// Releases the unmanaged resources used by this instance and
        /// optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                if (disposing)
                {
                    this.container.Dispose();
                }

                this.IsDisposed = true;
            }
        }

        /// <summary>
        /// Registers a service as being implemented by the specified type,
        /// ensuring that only a single instance of the implementing type will
        /// be created.
        /// </summary>
        /// <param name="service">The service type.</param>
        /// <param name="implementation">The type that implements the service.</param>
        protected virtual void RegisterSingleInstance(Type service, Type implementation)
        {
            this.container.Register(
                service,
                implementation,
                Reuse.Singleton,
                ifAlreadyRegistered: IfAlreadyRegistered.Keep,
                made: FactoryMethod.ConstructorWithResolvableArguments);
        }

        /// <summary>
        /// Registers a service as being implemented by the specified type.
        /// </summary>
        /// <param name="service">The service type.</param>
        /// <param name="implementation">The type that implements the service.</param>
        protected virtual void RegisterTransient(Type service, Type implementation)
        {
            this.container.Register(
                service,
                implementation,
                Reuse.Transient,
                ifAlreadyRegistered: IfAlreadyRegistered.AppendNewImplementation,
                made: FactoryMethod.ConstructorWithResolvableArguments,
                setup: Setup.With(trackDisposableTransient: true));
        }

        /// <summary>
        /// Returns an instance of the specified service.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <returns>An instance of the specified service.</returns>
        protected TService Resolve<TService>()
        {
            return (TService)this.Resolve(typeof(TService));
        }

        /// <summary>
        /// Returns an instance of the specified service.
        /// </summary>
        /// <param name="serviceType">The service type.</param>
        /// <returns>An instance of the specified service.</returns>
        protected virtual object Resolve(Type serviceType)
        {
            return this.container.Resolve(serviceType);
        }

        /// <summary>
        /// Raises the <see cref="ObjectDisposedException"/> if <see cref="Dispose()"/>
        /// has been called on this instance.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        private static bool IsImplementationType(Type type)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            return typeInfo.IsClass && !typeInfo.IsAbstract;
        }

        private void RegisterMany(Type[] services, Type implementation, bool isSingleInstance)
        {
            Action<Type, Type> register = isSingleInstance ?
                new Action<Type, Type>(this.RegisterSingleInstance) : this.RegisterTransient;

            for (int i = 0; i < services.Length; i++)
            {
                if (services[i] == typeof(IEnumerable<>))
                {
                    continue;
                }

                register(services[i], implementation);
            }
        }

        private TService TryResolve<TService, TFallback>()
            where TFallback : class, TService
        {
            TService[] services = this.Resolve<TService[]>();
            switch (services.Length)
            {
                case 0:
                    return this.Create<TFallback>();

                case 1:
                    return services[0];

                default:
                    throw new InvalidOperationException("Multiple services were found for " + typeof(TService).Name);
            }
        }
    }
}
