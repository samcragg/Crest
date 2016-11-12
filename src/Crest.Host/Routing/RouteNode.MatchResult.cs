// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System.Collections.Generic;

    /// <content>
    /// Contains the nested <see cref="MatchResult"/> struct.
    /// </content>
    internal sealed partial class RouteNode
    {
        /// <summary>
        /// Represents the result of matching a URL.
        /// </summary>
        internal struct MatchResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MatchResult"/> struct.
            /// </summary>
            /// <param name="captures">The captured parameter values.</param>
            /// <param name="value">The matched value.</param>
            internal MatchResult(IReadOnlyDictionary<string, object> captures, RouteMethod value)
            {
                this.Captures = captures;
                this.Value = value;
            }

            /// <summary>
            /// Gets a collection of parameter name/values that were captured
            /// as part of the matching.
            /// </summary>
            /// <remarks>
            /// This property will return <c>null</c> if <see cref="Success"/>
            /// returns <c>false</c>.
            /// </remarks>
            public IReadOnlyDictionary<string, object> Captures { get; }

            /// <summary>
            /// Gets a value indicating whether the match is successful.
            /// </summary>
            public bool Success => this.Captures != null;

            /// <summary>
            /// Gets the value stored against the route.
            /// </summary>
            public RouteMethod Value { get; }
        }
    }
}
