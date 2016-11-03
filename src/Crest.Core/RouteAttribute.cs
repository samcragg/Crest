// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Core
{
    using System;

    /// <summary>
    /// Allows a route to be defined on a method.
    /// </summary>
    public abstract class RouteAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RouteAttribute"/> class.
        /// </summary>
        /// <param name="route">Describes the route URL to match.</param>
        protected RouteAttribute(string route)
        {
            this.Route = route;
        }

        /// <summary>
        /// Gets the route to match.
        /// </summary>
        public string Route { get; }

        /// <summary>
        /// Gets the HTTP verb to match.
        /// </summary>
        public abstract string Verb { get; }
    }
}
