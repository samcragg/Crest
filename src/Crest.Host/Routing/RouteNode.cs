// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Groups route nodes together, optionally storing a method to invoke if
    /// this is a leaf node.
    /// </summary>
    internal sealed partial class RouteNode
    {
        private readonly IMatchNode matcher;
        private RouteNode[] children;
        private RouteMethod value;

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteNode"/> class.
        /// </summary>
        /// <param name="matcher">Used to match the part of the route.</param>
        public RouteNode(IMatchNode matcher)
        {
            this.matcher = matcher;
        }

        /// <summary>
        /// Gets or sets gets the value associated with this group.
        /// </summary>
        internal RouteMethod Value
        {
            get
            {
                return this.value;
            }
            set
            {
                Debug.Assert(this.value == null, "Value can only be set once.");
                this.value = value;
            }
        }

        /// <summary>
        /// Adds the specified node to this instance.
        /// </summary>
        /// <param name="nodes">The nodes to add.</param>
        /// <param name="index">The starting index of the first node to add.</param>
        /// <param name="value">The value to associate with the route.</param>
        public void Add(IReadOnlyList<IMatchNode> nodes, int index, RouteMethod value)
        {
            IMatchNode matcher = nodes[index];
            RouteNode node = null;
            if (this.children != null)
            {
                node = this.children.FirstOrDefault(n => n.matcher.Equals(matcher));
            }

            if (node == null)
            {
                node = new RouteNode(matcher);
                this.AddChild(node);
            }

            index++;
            if (index == nodes.Count)
            {
                node.Value = value;
            }
            else
            {
                node.Add(nodes, index, value);
            }
        }

        /// <summary>
        /// Determines whether the specified URL matches any added to this
        /// instance or not.
        /// </summary>
        /// <param name="url">The URL to match.</param>
        /// <returns>
        /// <c>true</c> if this instance matched the URL; otherwise, <c>false</c>.
        /// </returns>
        public MatchResult Match(string url)
        {
            var segments = new List<StringSegment>(UrlParser.GetSegments(url));
            var captures = new Dictionary<string, object>();
            RouteNode node = this.Match(segments, -1, captures);
            if (node == null)
            {
                return default(MatchResult);
            }
            else
            {
                return new MatchResult(captures, node.Value);
            }
        }

        private void AddChild(RouteNode node)
        {
            if (this.children == null)
            {
                this.children = new[] { node };
            }
            else
            {
                int length = this.children.Length;
                Array.Resize(ref this.children, length + 1);
                this.children[length] = node;

                // Sort largest first (hence b compare to a)
                Array.Sort(this.children, (a, b) => b.matcher.Priority.CompareTo(a.matcher.Priority));
            }
        }

        private RouteNode Match(IReadOnlyList<StringSegment> segments, int index, Dictionary<string, object> captures)
        {
            NodeMatchResult match = NodeMatchResult.None;
            if (index >= 0)
            {
                match = this.matcher.Match(segments[index]);
                if (!match.Success)
                {
                    return null;
                }
            }

            index++;
            RouteNode result = null;
            if (index == segments.Count)
            {
                result = this;
            }
            else
            {
                result = this.MatchChildren(segments, index, captures);
            }

            // Add this after we've searched out children, as during that search
            // we might clear the captures as we head down wrong paths
            if ((result != null) && (match.Name != null))
            {
                captures[match.Name] = match.Value;
            }

            return result;
        }

        private RouteNode MatchChildren(IReadOnlyList<StringSegment> segments, int index, Dictionary<string, object> captures)
        {
            if (this.children != null)
            {
                for (int i = 0; i < this.children.Length; i++)
                {
                    // Clear any previously captured parameters as we're searching
                    // down a new branch
                    captures.Clear();
                    RouteNode result = this.children[i].Match(segments, index, captures);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }

            return null;
        }
    }
}
