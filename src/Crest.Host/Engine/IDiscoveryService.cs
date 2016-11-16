// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Allows the discovery of assemblies to register for injection and
    /// methods to map to routes.
    /// </summary>
    public interface IDiscoveryService
    {
        /// <summary>
        /// Gets custom factory instances that can be used to create specific
        /// types.
        /// </summary>
        /// <returns>
        /// A sequence of factories that can create object instances.
        /// </returns>
        IEnumerable<ITypeFactory> GetCustomFactories();

        /// <summary>
        /// Scans for available types for injecting and routing.
        /// </summary>
        /// <returns>A sequence of discovered types.</returns>
        IEnumerable<Type> GetDiscoveredTypes();

        /// <summary>
        /// Gets the metadata for the routes to match.
        /// </summary>
        /// <param name="type">The type to scan for routes.</param>
        /// <returns>A sequence of route metadata.</returns>
        /// <remarks>
        /// The <see cref="RouteMetadata.Factory"/> property can be <c>null</c>
        /// to indicate that the default service provider of the bootstrapper
        /// should be used.
        /// </remarks>
        IEnumerable<RouteMetadata> GetRoutes(Type type);

        /// <summary>
        /// Determines whether the specified type should be created once and
        /// then reused for subsequent requests.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns>
        /// <c>true</c> if the type should only be created once per application
        /// run; otherwise, <c>false</c>.
        /// </returns>
        bool IsSingleInstance(Type type);
    }
}
