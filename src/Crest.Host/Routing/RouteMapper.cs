// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Crest.Host.Engine;

    /// <summary>
    /// Allows the matching of routes to their method.
    /// </summary>
    internal sealed partial class RouteMapper : IRouteMapper
    {
        // The method is stored against its MetadataToken so we can find it again
        private readonly Dictionary<int, RouteMethod> adapters = new Dictionary<int, RouteMethod>();

        private readonly Dictionary<string, RouteNode<Route>> verbs =
            new Dictionary<string, RouteNode<Route>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteMapper"/> class.
        /// </summary>
        /// <param name="routes">The routes to match.</param>
        public RouteMapper(IEnumerable<RouteMetadata> routes)
        {
            var adapter = new RouteMethodAdapter();
            var builder = new NodeBuilder();

            foreach (RouteMetadata metadata in routes)
            {
                IMatchNode[] nodes = builder.Parse(
                    MakeVersion(metadata),
                    metadata.RouteUrl,
                    metadata.Method.GetParameters());

                this.AddRoute(metadata.Verb, nodes, metadata);

                RouteMethod lambda = adapter.CreateMethod(metadata.Factory, metadata.Method);
                this.adapters[metadata.Method.MetadataToken] = lambda;
            }
        }

        /// <inheritdoc />
        public RouteMethod GetAdapter(MethodInfo method)
        {
            RouteMethod adapter;
            this.adapters.TryGetValue(method.MetadataToken, out adapter);
            return adapter;
        }

        /// <inheritdoc />
        public MethodInfo Match(string verb, string path, ILookup<string, string> query, out IReadOnlyDictionary<string, object> parameters)
        {
            RouteNode<Route> node;
            if (this.verbs.TryGetValue(verb, out node))
            {
                RouteNode<Route>.MatchResult match = node.Match(path);
                if (match.Success)
                {
                    int version = (int)match.Captures[VersionCaptureNode.KeyName];
                    MethodInfo method = match.Value.Match(version);
                    if (method != null)
                    {
                        parameters = match.Captures;
                        return method;
                    }
                }
            }

            parameters = null;
            return null;
        }

        private static string MakeVersion(RouteMetadata metadata)
        {
            string from = metadata.MinimumVersion.ToString(CultureInfo.InvariantCulture);
            string to = metadata.MaximumVersion.ToString(CultureInfo.InvariantCulture);
            return string.Concat(from, ":", to);
        }

        private void AddRoute(string verb, IMatchNode[] matches, RouteMetadata metadata)
        {
            RouteNode<Route> parent;
            if (!this.verbs.TryGetValue(verb, out parent))
            {
                parent = new RouteNode<Route>(new VersionCaptureNode());
                this.verbs.Add(verb, parent);
            }

            RouteNode<Route> node = parent.Add(matches, 0);
            if (node.Value == null)
            {
                node.Value = new Route();
            }

            node.Value.Add(metadata.Method, metadata.MinimumVersion, metadata.MaximumVersion);
        }
    }
}
