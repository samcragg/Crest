// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Parsing
{
    using System;
    using System.Collections.Generic;
    using Crest.Host.Routing.Captures;

    /// <summary>
    /// Allows the building of <see cref="RouteTrie{T}"/> objects.
    /// </summary>
    /// <typeparam name="T">The type of the value to store in the nodes.</typeparam>
    internal sealed partial class RouteTrieBuilder<T>
        where T : class
    {
        private readonly MutableNode root = new MutableNode(null);

        /// <summary>
        /// Adds the specified matchers to this instance.
        /// </summary>
        /// <param name="key">The matchers used to match a route.</param>
        /// <param name="value">The value to store for the matched route.</param>
        public void Add(IEnumerable<IMatchNode> key, T value)
        {
            MutableNode currentNode = this.root;
            foreach (IMatchNode matcher in key)
            {
                if (matcher is LiteralNode literal)
                {
                    currentNode = currentNode.Add(literal.Literal, 0);
                }
                else
                {
                    currentNode = AddOrGetChildWithMatcher(currentNode.Children, matcher);
                }
            }

            if (!currentNode.AddValue(value))
            {
                throw new InvalidOperationException("Ambiguous route");
            }
        }

        /// <summary>
        /// Builds a <see cref="RouteTrie{T}"/> that can match all the routes
        /// added to this instance.
        /// </summary>
        /// <returns>A new <see cref="RouteTrie{T}"/>.</returns>
        public RouteTrie<T> Build()
        {
            var children = new List<RouteTrie<T>>();
            foreach (MutableNode child in this.root.Children)
            {
                ConvertNode(string.Empty, child, children);
            }

            return new RouteTrie<T>(new VersionCaptureNode(), Array.Empty<T>(), children.ToArray());
        }

        private static void AddNode(string prefix, MutableNode node, List<RouteTrie<T>> output, RouteTrie<T>[] children)
        {
            if (node.Matcher != null)
            {
                var matcherNode = new RouteTrie<T>(node.Matcher, node.GetValues(), children);

                // If we have a matcher then we can't be merged with the
                // previous node text, so create a new one to match it
                if (prefix.Length > 0)
                {
                    output.Add(new RouteTrie<T>(
                        new LiteralNode(prefix),
                        Array.Empty<T>(),
                        new[] { matcherNode }));
                }
                else
                {
                    output.Add(matcherNode);
                }
            }
            else
            {
                output.Add(new RouteTrie<T>(
                    new LiteralNode(prefix + node.Key),
                    node.GetValues(),
                    children));
            }
        }

        private static MutableNode AddOrGetChildWithMatcher(ICollection<MutableNode> children, IMatchNode matcher)
        {
            foreach (MutableNode child in children)
            {
                if (matcher.Equals(child.Matcher))
                {
                    return child;
                }
            }

            var newChild = new MutableNode(matcher);
            children.Add(newChild);
            return newChild;
        }

        // Given:
        //   abc   1
        //   abcde 2
        //   abcdf 3
        //
        // We'd have built the following nodes:
        //
        // a -> b -> c -> d -> e
        //           |    |    |
        //           1    f    2
        //                |
        //                3
        //
        // We now want to collapse that into:
        //
        //              f[3]
        // abc[1] - d <
        //              e[2]
        private static void ConvertNode(string prefix, MutableNode node, List<RouteTrie<T>> output)
        {
            if (node.IsEdgeNode || (node.Children.Count > 1))
            {
                var convertedChildren = new List<RouteTrie<T>>();
                foreach (MutableNode child in node.Children)
                {
                    ConvertNode(string.Empty, child, convertedChildren);
                }

                AddNode(prefix, node, output, convertedChildren.ToArray());
            }
            else if (node.Children.Count == 1)
            {
                // Skip this node - if we only have one child and we don't have
                // a value then merge this node with the child by adding its
                // Key to the prefix we're building
                ConvertNode(prefix + node.Key, node.Children[0], output);
            }
        }
    }
}
