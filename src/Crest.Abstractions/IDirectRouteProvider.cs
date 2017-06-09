// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Allows the specification of URLs that skip the routing pipeline.
    /// </summary>
    public interface IDirectRouteProvider
    {
        /// <summary>
        /// Gets routes which should bypass the normal processing pipeline.
        /// </summary>
        /// <returns>A sequence of route metadata.</returns>
        IEnumerable<DirectRouteMetadata> GetDirectRoutes();
    }
}
