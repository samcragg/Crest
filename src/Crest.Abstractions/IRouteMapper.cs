// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Maps the routes to their methods.
    /// </summary>
    public interface IRouteMapper
    {
        /// <summary>
        /// Looks for an override method for the specified route.
        /// </summary>
        /// <param name="verb">The HTTP verb.</param>
        /// <param name="path">The URL path.</param>
        /// <returns>
        /// A delegate to invoke if the route has been overridden, or
        /// <c>null</c> if there are no overrides for the specified route.
        /// </returns>
        OverrideMethod FindOverride(string verb, string path);

        /// <summary>
        /// Gets the adapter for the method returned by <see cref="Match"/>.
        /// </summary>
        /// <param name="method">The previously returned method info.</param>
        /// <returns>
        /// An adapter that can be used to invoke the specified method, or
        /// <c>null</c> if the method was not found.
        /// </returns>
        RouteMethod GetAdapter(MethodInfo method);

        /// <summary>
        /// Gets the methods that have been mapped.
        /// </summary>
        /// <returns>A series of mapped methods.</returns>
        IEnumerable<MethodInfo> GetKnownMethods();

        /// <summary>
        /// Matches the request information to a handler.
        /// </summary>
        /// <param name="verb">The HTTP verb.</param>
        /// <param name="path">The URL path.</param>
        /// <param name="query">Contains the query parameters.</param>
        /// <param name="parameters">
        /// When this method returns, contains the parsed parameters to pass to
        /// the handler.
        /// </param>
        /// <returns>
        /// The method to invoke to handle the request, or null if no handler
        /// is found.
        /// </returns>
        MethodInfo Match(string verb, string path, ILookup<string, string> query, out IReadOnlyDictionary<string, object> parameters);
    }
}
