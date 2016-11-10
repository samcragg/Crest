// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Maps the routes to their methods.
    /// </summary>
    internal interface IRouteMapper
    {
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
