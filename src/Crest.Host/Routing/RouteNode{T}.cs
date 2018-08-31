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
    /// <typeparam name="T">The type of th value to store.</typeparam>
    internal sealed partial class RouteNode<T>
    {
        private readonly IMatchNode matcher;
        private RouteNode<T>[] children;
        private T value;

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteNode{T}"/> class.
        /// </summary>
        /// <param name="matcher">Used to match the part of the route.</param>
        public RouteNode(IMatchNode matcher)
        {
            this.matcher = matcher;
        }

        /// <summary>
        /// Gets or sets gets the value associated with this group.
        /// </summary>
        internal T Value
        {
            get => this.value;
            set
            {
                Debug.Assert(this.value == null, "Value can only be set once.");
                this.value = value;
            }
        }

        /// <summary>
        /// Adds the specified nodes to this instance.
        /// </summary>
        /// <param name="nodes">The nodes to add.</param>
        /// <param name="index">The starting index of the first node to add.</param>
        /// <returns>
        /// The leaf node that can store the value for the route (i.e. the
        /// <c>RouteNode</c> that represents the last item in <c>nodes</c>).
        /// </returns>
        public RouteNode<T> Add(IReadOnlyList<IMatchNode> nodes, int index)
        {
            IMatchNode currentMatchNode = nodes[index];
            RouteNode<T> node = null;
            if (this.children != null)
            {
                node = this.children.FirstOrDefault(n => n.matcher.Equals(currentMatchNode));
            }

            if (node == null)
            {
                node = new RouteNode<T>(currentMatchNode);
                this.AddChild(node);
            }

            index++;
            if (index == nodes.Count)
            {
                return node;
            }
            else
            {
                return node.Add(nodes, index);
            }
        }

        /// <summary>
        /// Gets this node and all the child nodes of the tree.
        /// </summary>
        /// <returns>A sequence of nodes.</returns>
        public IEnumerable<RouteNode<T>> Flatten()
        {
            var stack = new Stack<RouteNode<T>>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                RouteNode<T> node = stack.Pop();
                if (node.children != null)
                {
                    foreach (RouteNode<T> child in node.children)
                    {
                        stack.Push(child);
                    }
                }

                yield return node;
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
            (int start, int length)[] segments = UrlParser.GetSegments(url);
            if (segments.Length > 0)
            {
                var captures = new StringDictionary<object>();
                RouteNode<T> node = this.Match(url, segments, 0, captures);
                if (node != null)
                {
                    return new MatchResult(captures, node.Value);
                }
            }

            return default;
        }

        private void AddChild(RouteNode<T> node)
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

        private RouteNode<T> Match(string url, (int, int)[] segments, int index, IDictionary<string, object> captures)
        {
            (int start, int length) = segments[index];
            NodeMatchResult match = this.matcher.Match(url.AsSpan(start, length));
            if (!match.Success)
            {
                return null;
            }

            index++;
            RouteNode<T> result = null;
            if (index == segments.Length)
            {
                result = this;
            }
            else
            {
                result = this.MatchChildren(url, segments, index, captures);
            }

            // Add this after we've searched out children, as during that search
            // we might clear the captures as we head down wrong paths
            if ((result != null) && !string.IsNullOrEmpty(match.Name))
            {
                captures.Add(match.Name, match.Value);
            }

            return result;
        }

        private RouteNode<T> MatchChildren(string url, (int, int)[] segments, int index, IDictionary<string, object> captures)
        {
            if (this.children != null)
            {
                for (int i = 0; i < this.children.Length; i++)
                {
                    // Clear any previously captured parameters as we're searching
                    // down a new branch
                    captures.Clear();
                    RouteNode<T> result = this.children[i].Match(url, segments, index, captures);
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
