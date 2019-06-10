// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;
    using static System.Diagnostics.Debug;

    /// <summary>
    /// Allows the matching of a sequence of characters to a value.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    internal partial class RouteTrie<T>
        where T : class
    {
        private readonly RouteTrie<T>[] children;
        private readonly IMatchNode prefix;
        private readonly T[] values;

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteTrie{T}"/> class.
        /// </summary>
        /// <param name="prefix">Used to match the start of the sequence.</param>
        /// <param name="values">The values to return for a full match.</param>
        /// <param name="children">
        /// Any child nodes used to match the rest of the sequence.
        /// </param>
        public RouteTrie(IMatchNode prefix, T[] values, RouteTrie<T>[] children)
        {
            Assert(prefix != null, "Value cannot be null");
            Assert(values != null, "Value cannot be null");
            Assert(children != null, "Value cannot be null");

            this.children = children;
            this.prefix = prefix;
            this.values = values;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteTrie{T}"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is only used to allow the type to be mocked in unit tests.
        /// </remarks>
        protected RouteTrie()
        {
        }

        /// <summary>
        /// Determines whether the specified sequence is matched by this instance.
        /// </summary>
        /// <param name="key">The sequence to match.</param>
        /// <returns>Information about the matched value.</returns>
        public virtual MatchResult Match(ReadOnlySpan<char> key)
        {
            var captures = new Dictionary<string, object>();
            RouteTrie<T> matchedNode = this.FindMatch(key, captures);
            if (matchedNode != null)
            {
                return new MatchResult(captures, matchedNode.values);
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Gets all the nodes of the tree.
        /// </summary>
        /// <param name="depth">The current depth of traversal.</param>
        /// <returns>The sequence of node values.</returns>
        internal virtual IEnumerable<(int depth, IMatchNode node, T[] values)> GetNodes(int depth = 0)
        {
            yield return (depth, this.prefix, this.values);

            foreach (RouteTrie<T> child in this.children)
            {
                foreach ((int d, IMatchNode n, T[] v) in child.GetNodes(depth + 1))
                {
                    yield return (d, n, v);
                }
            }
        }

        private RouteTrie<T> FindMatch(ReadOnlySpan<char> key, IDictionary<string, object> captures)
        {
            NodeMatchInfo match = this.prefix.Match(key);
            if (!match.Success)
            {
                return null;
            }

            RouteTrie<T> matchedNode;
            if (key.Length == match.MatchLength)
            {
                matchedNode = this;
            }
            else
            {
                matchedNode = this.FindMatchingChild(key.Slice(match.MatchLength), captures);
            }

            // Do this at the end as FindMatchingChild can clear the captures
            if (match.HasCapture)
            {
                captures[match.Parameter] = match.Value;
            }

            return matchedNode;
        }

        private RouteTrie<T> FindMatchingChild(in ReadOnlySpan<char> key, IDictionary<string, object> captures)
        {
            foreach (RouteTrie<T> child in this.children)
            {
                captures.Clear();
                RouteTrie<T> matchedNode = child.FindMatch(key, captures);
                if (matchedNode != null)
                {
                    return matchedNode;
                }
            }

            return null;
        }
    }
}
