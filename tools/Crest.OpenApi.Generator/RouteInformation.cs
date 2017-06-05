// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System.Reflection;

    /// <summary>
    /// Contains information about a route definition.
    /// </summary>
    internal sealed class RouteInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RouteInformation"/> class.
        /// </summary>
        /// <param name="verb">The HTTP verb.</param>
        /// <param name="route">The URL.</param>
        /// <param name="method">The method.</param>
        /// <param name="min">The minimum version.</param>
        /// <param name="max">The maximum version.</param>
        public RouteInformation(string verb, string route, MethodInfo method, int min, int max)
        {
            this.MaxVersion = max;
            this.Method = method;
            this.MinVersion = min;
            this.Route = route;
            this.Verb = verb;
        }

        /// <summary>
        /// Gets the maximum version the route is available until (inclusive).
        /// </summary>
        public int MaxVersion { get; }

        /// <summary>
        /// Gets the method to invoke for the route.
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// Gets the minimum version the route is available from (inclusive).
        /// </summary>
        public int MinVersion { get; }

        /// <summary>
        /// Gets the route URL.
        /// </summary>
        public string Route { get; }

        /// <summary>
        /// Gets the HTTP verb.
        /// </summary>
        public string Verb { get; }
    }
}
