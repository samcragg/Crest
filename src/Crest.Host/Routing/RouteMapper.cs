// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Allows the matching of routes to their method.
    /// </summary>
    internal sealed class RouteMapper : IRouteMapper
    {
        private readonly Dictionary<int, RouteMethod> adapters = new Dictionary<int, RouteMethod>();

        private readonly Dictionary<string, RouteNode<MethodInfo>> verbs =
            new Dictionary<string, RouteNode<MethodInfo>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteMapper"/> class.
        /// </summary>
        /// <param name="routes">The routes to match.</param>
        public RouteMapper(IEnumerable<RouteMetadata> routes)
        {
            var adapter = new RouteMethodAdapter();
            var builder = new NodeBuilder();

            foreach (RouteMetadata route in routes)
            {
                IMatchNode[] nodes = builder.Parse(route.RouteUrl, route.Method.GetParameters());
                this.AddRoute(route.Verb, nodes, route.Method);

                RouteMethod lambda = adapter.CreateMethod(route.Factory, route.Method);
                this.adapters[route.Method.MetadataToken] = lambda;
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
            RouteNode<MethodInfo> node;
            if (this.verbs.TryGetValue(verb, out node))
            {
                RouteNode<MethodInfo>.MatchResult match = node.Match(path);
                if (match.Success)
                {
                    parameters = match.Captures;
                    return match.Value;
                }
            }

            parameters = null;
            return null;
        }

        private void AddRoute(string verb, IMatchNode[] nodes, MethodInfo method)
        {
            RouteNode<MethodInfo> parent;
            if (!this.verbs.TryGetValue(verb, out parent))
            {
                parent = new RouteNode<MethodInfo>(null);
                this.verbs.Add(verb, parent);
            }

            parent.Add(nodes, 0, method);
        }
    }
}
