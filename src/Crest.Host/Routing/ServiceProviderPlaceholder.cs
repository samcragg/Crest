// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;

    /// <summary>
    /// Allows a service provider to be placed in the captures dictionary.
    /// </summary>
    /// <remarks>
    /// Because we scope the service provider when the request is being
    /// processed, we need a way of updating the service provider stored in the
    /// read-only dictionary. Therefore, this instance is stored in the
    /// dictionary and it can be updated lated to point to the real value.
    /// </remarks>
    internal class ServiceProviderPlaceholder : IServiceProvider
    {
        /// <summary>
        /// Gets the key for the service provider in the parameters dictionary.
        /// </summary>
        internal const string Key = "__container__";

        /// <summary>
        /// Gets or sets the service provider used to resolve services.
        /// </summary>
        public IServiceProvider Provider { get; set; }

        /// <inheritdoc />
        public object GetService(Type serviceType)
        {
            return this.Provider?.GetService(serviceType);
        }
    }
}
