// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;
    using System.Collections.Generic;
    using Crest.Host.Conversion;
    using DryIoc;

    /// <summary>
    /// Creates the instances of interfaces required during initialization.
    /// </summary>
    public class ServiceLocator : IServiceRegister
    {
        private readonly IContainer container;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceLocator"/> class.
        /// </summary>
        public ServiceLocator()
            : this(new Container())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceLocator"/> class.
        /// </summary>
        /// <param name="container">The container to use.</param>
        /// <remarks>This constructor is to allow unit testing.</remarks>
        internal ServiceLocator(IContainer container)
        {
            this.container = container;
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
        public ConfigurationService GetConfigurationService()
        {
            this.ThrowIfDisposed();

            var providers = this.Resolve<IEnumerable<IConfigurationProvider>>();
            return new ConfigurationService(providers);
        }

        /// <inheritdoc />
        public IContentConverterFactory GetContentConverterFactory()
        {
            this.ThrowIfDisposed();

            return this.TryResolve<IContentConverterFactory, ContentConverterFactory>();
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
        public IHtmlTemplateProvider GetHtmlTemplateProvider()
        {
            this.ThrowIfDisposed();

            return this.TryResolve<IHtmlTemplateProvider, HtmlTemplateProvider>();
        }

        /// <inheritdoc />
        public IResponseStatusGenerator GetResponseStatusGenerator()
        {
            this.ThrowIfDisposed();

            return this.TryResolve<IResponseStatusGenerator, ResponseGenerator>();
        }

        /// <inheritdoc />
        public object GetService(Type serviceType)
        {
            Check.IsNotNull(serviceType, nameof(serviceType));
            this.ThrowIfDisposed();

            return this.Resolve(serviceType);
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
        public void RegisterInitializer(Action<object> initialize, Func<Type, bool> condition)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RegisterMany(IEnumerable<Type> types, Func<Type, bool> isSingleInstance)
        {
            throw new NotImplementedException();
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

        //// /// <summary>
        //// /// Registers a service as being implemented by the specified type.
        //// /// </summary>
        //// /// <typeparam name="TService">The service type.</typeparam>
        //// /// <typeparam name="TImplementation">
        //// /// The type that implements the service.
        //// /// </typeparam>
        //// protected virtual void RegisterTransient<TService, TImplementation>()
        ////     where TImplementation : TService
        //// {
        ////     this.container.Register<TService, TImplementation>();
        //// }

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
