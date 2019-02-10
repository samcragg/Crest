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
    /// Represents the result of a call to
    /// <see cref="IRouteMapper.Match(string, string, ILookup{string, string})"/>.
    /// </summary>
    public sealed class RouteMapperMatchResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RouteMapperMatchResult"/> class.
        /// </summary>
        /// <param name="method">The matched method.</param>
        /// <param name="parameters">The captured parameters.</param>
        public RouteMapperMatchResult(
            MethodInfo method,
            IReadOnlyDictionary<string, object> parameters)
        {
            this.Method = method;
            this.Parameters = parameters;
        }

        /// <summary>
        /// Gets the matched method.
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// Gets the captured parameters.
        /// </summary>
        public IReadOnlyDictionary<string, object> Parameters { get; }
    }
}
