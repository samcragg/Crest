// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Host.Engine;
    using Crest.Host.Routing;
    using DryIoc;

    /// <summary>
    /// Allows the configuration of the Crest framework during application
    /// startup.
    /// </summary>
    public abstract partial class Bootstrapper : IDisposable
    {
        private readonly ContainerAdapter adapter = new ContainerAdapter();
        private readonly ServiceLocator serviceLocator;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bootstrapper"/> class.
        /// </summary>
        protected Bootstrapper()
            : this(new ServiceLocator())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bootstrapper"/> class.
        /// </summary>
        /// <param name="serviceLocator">Used to locate the services.</param>
        protected Bootstrapper(ServiceLocator serviceLocator)
        {
            Check.IsNotNull(serviceLocator, nameof(serviceLocator));
            this.serviceLocator = serviceLocator;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="Bootstrapper"/> class.
        /// </summary>
        ~Bootstrapper()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Gets the instance to use to resolve services.
        /// </summary>
        public ServiceLocator ServiceLocator
        {
            get
            {
                this.ThrowIfDisposed();
                return this.serviceLocator;
            }
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
        /// Gets an object that can be used to create other objects.
        /// </summary>
        protected virtual IServiceProvider ServiceProvider
        {
            get { return this.adapter; }
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
        /// Resolves the specified service.
        /// </summary>
        /// <typeparam name="T">The type of the service to resolve.</typeparam>
        /// <returns>An instance of the specified type.</returns>
        public virtual T GetService<T>()
        {
            this.ThrowIfDisposed();

            return (T)this.ServiceProvider.GetService(typeof(T));
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
                    this.adapter.Container.Dispose();
                    this.serviceLocator.Dispose();
                }

                this.IsDisposed = true;
            }
        }

        /// <summary>
        /// Initializes the container and routes.
        /// </summary>
        protected void Initialize()
        {
            IDiscoveryService discovery = this.serviceLocator.GetDiscoveryService();
            IReadOnlyCollection<Type> types = this.RegisterTypes(discovery);

            List<RouteMetadata> routes =
                types.SelectMany(t => this.GetRoutes(discovery, t)).ToList();
            this.RegisterInstance(typeof(IRouteMapper), new RouteMapper(routes));

            ConfigurationService configuration = this.serviceLocator.GetConfigurationService();
            configuration.InitializeProviders(types).Wait();
            this.adapter.Container.RegisterInitializer<object>(
                (instance, _) => configuration.InitializeInstance(instance, this.ServiceProvider),
                r => configuration.CanConfigure(r.ServiceType));
        }

        /// <summary>
        /// Registers a specific instance against a service type.
        /// </summary>
        /// <param name="service">The type of the service.</param>
        /// <param name="instance">
        /// The object instance to return when asked for the specified service.
        /// </param>
        protected virtual void RegisterInstance(Type service, object instance)
        {
            this.adapter.Container.Unregister(service);
            this.adapter.Container.UseInstance(service, instance);
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

        private Func<IResolver, object> GetFactory(ITypeFactory[] factories, Type type)
        {
            for (int i = 0; i < factories.Length; i++)
            {
                // Assign to local so that the lambda doesn't capture the whole array
                ITypeFactory factory = factories[i];
                if (factory.CanCreate(type))
                {
                    return _ => factory.Create(type, this.adapter);
                }
            }

            return null;
        }

        private IEnumerable<RouteMetadata> GetRoutes(IDiscoveryService discovery, Type type)
        {
            foreach (RouteMetadata route in discovery.GetRoutes(type))
            {
                route.Factory = route.Factory ?? (() => this.ServiceProvider.GetService(type));
                yield return route;
            }
        }

        private IReadOnlyCollection<Type> RegisterTypes(IDiscoveryService discovery)
        {
            ITypeFactory[] factories = discovery.GetCustomFactories().ToArray();
            var normal = new List<Type>();
            var custom = new List<Type>(); // We need to store these to return them at the end

            foreach (Type type in discovery.GetDiscoveredTypes())
            {
                Func<IResolver, object> factory = this.GetFactory(factories, type);
                if (factory == null)
                {
                    normal.Add(type);
                }
                else
                {
                    this.adapter.Container.RegisterDelegate(
                        type,
                        factory,
                        ifAlreadyRegistered: IfAlreadyRegistered.Replace);

                    custom.Add(type);
                }
            }

            var helper = new RegisterHelper(discovery);
            helper.RegisterMany(this.adapter.Container, normal);

            // Normal probably has the most types in it, so fold the others into it
            normal.AddRange(custom);
            return normal;
        }
    }
}
