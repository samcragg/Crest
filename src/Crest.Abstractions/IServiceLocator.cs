// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System;

    /// <summary>
    /// Creates the instances of interfaces required during initialization.
    /// </summary>
    public interface IServiceLocator : IServiceProvider
    {
        /// <summary>
        /// Creates a service provider that can create services for the
        /// duration of a request.
        /// </summary>
        /// <returns>A child service provider.</returns>
        IServiceLocator CreateScope();

        /// <summary>
        /// Gets the registered plugins to call after processing a request.
        /// </summary>
        /// <returns>A sequence of registered plugins.</returns>
        IPostRequestPlugin[] GetAfterRequestPlugins();

        /// <summary>
        /// Gets the registered plugins to call before processing a request.
        /// </summary>
        /// <returns>A sequence of registered plugins.</returns>
        IPreRequestPlugin[] GetBeforeRequestPlugins();

        /// <summary>
        /// Gets the registered plugins to call to obtain direct routes.
        /// </summary>
        /// <returns>A sequence of registered plugins.</returns>
        IDirectRouteProvider[] GetDirectRouteProviders();

        /// <summary>
        /// Gets the service to use for discovering assemblies and types.
        /// </summary>
        /// <returns>An object implementing <see cref="IDiscoveryService"/>.</returns>
        IDiscoveryService GetDiscoveryService();

        /// <summary>
        /// Gets the registered plugins to handle generated exceptions.
        /// </summary>
        /// <returns>A sequence of registered plugins.</returns>
        IErrorHandlerPlugin[] GetErrorHandlers();

        /// <summary>
        /// Gets the services that need to be called during application startup.
        /// </summary>
        /// <returns>A sequence of registered initializers.</returns>
        IStartupInitializer[] GetInitializers();

        /// <summary>
        /// Gets the service to use to register services.
        /// </summary>
        /// <returns>An object implementing <see cref="IServiceRegister"/>.</returns>
        IServiceRegister GetServiceRegister();
    }
}
