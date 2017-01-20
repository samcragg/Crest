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
    public class ServiceLocator : IDisposable
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

        /// <summary>
        /// Gets the registered plugins to call after processing a request.
        /// </summary>
        /// <returns>A sequence of registered plugins.</returns>
        public virtual IPostRequestPlugin[] GetAfterRequestPlugins()
        {
            this.ThrowIfDisposed();

            return this.Resolve<IPostRequestPlugin[]>();
        }

        /// <summary>
        /// Gets the registered plugins to call before processing a request.
        /// </summary>
        /// <returns>A sequence of registered plugins.</returns>
        public virtual IPreRequestPlugin[] GetBeforeRequestPlugins()
        {
            this.ThrowIfDisposed();

            return this.Resolve<IPreRequestPlugin[]>();
        }

        /// <summary>
        /// Gets the service to use for providing configuration data.
        /// </summary>
        /// <returns>An object based on <see cref="ConfigurationService"/>.</returns>
        public virtual ConfigurationService GetConfigurationService()
        {
            this.ThrowIfDisposed();

            var providers = this.Resolve<IEnumerable<IConfigurationProvider>>();
            return new ConfigurationService(providers);
        }

        /// <summary>
        /// Gets the service to use for providing content converters for requests.
        /// </summary>
        /// <returns>An object implementing <see cref="IContentConverterFactory"/>.</returns>
        public virtual IContentConverterFactory GetContentConverterFactory()
        {
            this.ThrowIfDisposed();

            return this.TryResolve<IContentConverterFactory, ContentConverterFactory>();
        }

        /// <summary>
        /// Gets the service to use for discovering assemblies and types.
        /// </summary>
        /// <returns>An object implementing <see cref="IDiscoveryService"/>.</returns>
        public virtual IDiscoveryService GetDiscoveryService()
        {
            this.ThrowIfDisposed();

            return this.TryResolve<IDiscoveryService, DiscoveryService>();
        }

        /// <summary>
        /// Gets the registered plugins to handle generated exceptions.
        /// </summary>
        /// <returns>A sequence of registered plugins.</returns>
        public virtual IErrorHandlerPlugin[] GetErrorHandlers()
        {
            this.ThrowIfDisposed();

            return this.Resolve<IErrorHandlerPlugin[]>();
        }

        /// <summary>
        /// Gets the service to use for providing the template for generated HTML.
        /// </summary>
        /// <returns>An object implementing <see cref="IHtmlTemplateProvider"/>.</returns>
        public virtual IHtmlTemplateProvider GetHtmlTemplateProvider()
        {
            this.ThrowIfDisposed();

            return this.TryResolve<IHtmlTemplateProvider, HtmlTemplateProvider>();
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
        protected virtual TService Resolve<TService>()
        {
            return this.container.Resolve<TService>();
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
