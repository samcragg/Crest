// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;

    /// <summary>
    /// Allows the matching of parts of a route.
    /// </summary>
    internal interface IMatchNode : IEquatable<IMatchNode>, IQueryValueConverter
    {
        /// <summary>
        /// Gets the priority of this match when deciding between nodes that
        /// match the same content.
        /// </summary>
        /// <remarks>
        /// Higher priority (bigger values) should take precedence over nodes
        /// that match with a lower priority (smaller value).
        /// </remarks>
        int Priority { get; }

        /// <summary>
        /// Determines whether the specified string is matched by this instance.
        /// </summary>
        /// <param name="text">The string to match.</param>
        /// <returns>
        /// The result of the matching, including any captured parameters if
        /// successful.
        /// </returns>
        NodeMatchInfo Match(ReadOnlySpan<char> text);
    }
}
