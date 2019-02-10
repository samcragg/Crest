// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System.Collections.Generic;

    /// <content>
    /// Contains the nested helper <see cref="MatchResult"/> struct.
    /// </content>
    internal partial class RouteTrie<T>
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
            /// <param name="values">The matched values.</param>
            internal MatchResult(IReadOnlyDictionary<string, object> captures, T[] values)
            {
                this.Captures = captures;
                this.Values = values;
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
            /// Gets the values stored against the route.
            /// </summary>
            public T[] Values { get; }
        }
    }
}
