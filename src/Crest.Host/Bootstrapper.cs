// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System.Collections.Generic;

    /// <summary>
    /// Allows the configuration of the Crest framework during application
    /// startup.
    /// </summary>
    public abstract class Bootstrapper
    {
        /// <summary>
        /// Gets the registered plugins to call after processing a request.
        /// </summary>
        /// <returns>A sequence of registered plugins.</returns>
        public virtual IPostRequestPlugin[] GetAfterRequestPlugins()
        {
            return null;
        }

        /// <summary>
        /// Gets the registered plugins to call before processing a request.
        /// </summary>
        /// <returns>A sequence of registered plugins.</returns>
        public virtual IPreRequestPlugin[] GetBeforeRequestPlugins()
        {
            return null;
        }

        /// <summary>
        /// Gets the registered plugins to handle generated exceptions.
        /// </summary>
        /// <returns>A sequence of registered plugins.</returns>
        public virtual IErrorHandlerPlugin[] GetErrorHandlers()
        {
            return null;
        }

        /// <summary>
        /// Gets the metadata for the routes to match.
        /// </summary>
        /// <returns>A sequence of route metadata.</returns>
        public virtual IEnumerable<RouteMetadata> GetRoutes()
        {
            return null;
        }

        /// <summary>
        /// Resolves the specified service.
        /// </summary>
        /// <typeparam name="T">The type of the service to resolve.</typeparam>
        /// <returns>An instance of the specified type.</returns>
        public virtual T GetService<T>()
        {
            return default(T);
        }
    }
}
