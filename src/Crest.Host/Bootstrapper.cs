// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
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
    }
}
