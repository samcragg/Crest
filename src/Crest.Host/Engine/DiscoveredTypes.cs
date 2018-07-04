// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Contains the types that were discovered by the bootstrapper.
    /// </summary>
    /// <remarks>
    /// This class is solely here to allow the discovered types to be passed
    /// around in the dependency container.
    /// </remarks>
    internal sealed class DiscoveredTypes
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveredTypes"/> class.
        /// </summary>
        /// <param name="types">The discovered types.</param>
        public DiscoveredTypes(IReadOnlyCollection<Type> types)
        {
            this.Types = types.ToArray();
        }

        /// <summary>
        /// Gets the discovered types.
        /// </summary>
        public Type[] Types { get; }
    }
}
