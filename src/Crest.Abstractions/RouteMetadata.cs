// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Provides information about a route to match.
    /// </summary>
    public sealed class RouteMetadata
    {
        /// <summary>
        /// Gets or sets a value indicating whether the method can accept
        /// parameters from the request body or not.
        /// </summary>
        public bool CanReadBody { get; set; }

        /// <summary>
        /// Gets or sets the factory to use to create an instance of the object
        /// that <see cref="Method"/> is invoked on.
        /// </summary>
        /// <remarks>
        /// If <c>null</c> then the instance will be created from a service
        /// container that is scoped to the current request.
        /// </remarks>
        public Func<object> Factory { get; set; }

        /// <summary>
        /// Gets or sets the latest version (inclusive) the route is
        /// available until.
        /// </summary>
        public int MaximumVersion { get; set; }

        /// <summary>
        /// Gets or sets the method to invoke when the route is matched.
        /// </summary>
        public MethodInfo Method { get; set; }

        /// <summary>
        /// Gets or sets the earliest version (inclusive) the route is
        /// available from.
        /// </summary>
        public int MinimumVersion { get; set; }

        /// <summary>
        /// Gets or sets the URL to match.
        /// </summary>
        public string RouteUrl { get; set; }

        /// <summary>
        /// Gets or sets the HTTP verb to match.
        /// </summary>
        public string Verb { get; set; }
    }
}
