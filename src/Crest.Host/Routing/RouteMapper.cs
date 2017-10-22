// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using Crest.Abstractions;

    /// <summary>
    /// Allows the matching of routes to their method.
    /// </summary>
    internal sealed partial class RouteMapper : IRouteMapper
    {
        // The method is stored against its MetadataToken so we can find it again
        private readonly Dictionary<int, RouteMethod> adapters = new Dictionary<int, RouteMethod>();

        private readonly StringDictionary<Routes> verbs = new StringDictionary<Routes>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteMapper"/> class.
        /// </summary>
        /// <param name="routes">The routes to match.</param>
        /// <param name="overrides">Optional override routes.</param>
        public RouteMapper(
            IEnumerable<RouteMetadata> routes,
            IEnumerable<DirectRouteMetadata> overrides)
        {
            var adapter = new RouteMethodAdapter();
            var builder = new NodeBuilder();

            foreach (DirectRouteMetadata metadata in overrides)
            {
                this.AddDirectRoute(metadata.Verb, metadata.RouteUrl, metadata.Method);
            }

            foreach (RouteMetadata metadata in routes)
            {
                NodeBuilder.IParseResult result = builder.Parse(
                    MakeVersion(metadata),
                    metadata.RouteUrl,
                    metadata.Method.GetParameters());

                var target = new Target(metadata.Method, result.QueryCaptures);
                this.AddRoute(ref target, result.Nodes, metadata);

                RouteMethod lambda = adapter.CreateMethod(metadata.Factory, metadata.Method);
                this.adapters[metadata.Method.MetadataToken] = lambda;
            }
        }

        /// <inheritdoc />
        public OverrideMethod FindOverride(string verb, string path)
        {
            if (this.verbs.TryGetValue(verb, out Routes routes))
            {
                return routes.Overrides[path];
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc />
        public RouteMethod GetAdapter(MethodInfo method)
        {
            this.adapters.TryGetValue(method.MetadataToken, out RouteMethod adapter);
            return adapter;
        }

        /// <inheritdoc />
        public IEnumerable<MethodInfo> GetKnownMethods()
        {
            return this.verbs.Values
                .SelectMany(r => r.Root.Flatten())
                .Where(n => n.Value != null)
                .SelectMany(n => n.Value.Targets)
                .Select(t => t.Method);
        }

        /// <inheritdoc />
        public MethodInfo Match(string verb, string path, ILookup<string, string> query, out IReadOnlyDictionary<string, object> parameters)
        {
            if (this.verbs.TryGetValue(verb, out Routes routes))
            {
                RouteNode<Versions>.MatchResult match = routes.Root.Match(path);
                if (match.Success)
                {
                    int version = (int)match.Captures[VersionCaptureNode.KeyName];
                    Target target = match.Value.Match(version);
                    if (target.Method != null)
                    {
                        SaveQueryParameters(query, ref target, ref match);
                        parameters = match.Captures;
                        return target.Method;
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

        private static void SaveQueryParameters(ILookup<string, string> query, ref Target target, ref RouteNode<Versions>.MatchResult match)
        {
            if (target.QueryCaptures != null)
            {
                foreach (QueryCapture capture in target.QueryCaptures)
                {
                    capture.ParseParameters(query, match.Captures);
                }
            }
        }

        private void AddDirectRoute(string verb, string route, OverrideMethod method)
        {
            if (!this.verbs.TryGetValue(verb, out Routes parent))
            {
                parent = new Routes();
                this.verbs.Add(verb, parent);
            }

            parent.Overrides.Add(route, method);
        }

        private void AddRoute(ref Target target, IReadOnlyList<IMatchNode> matches, RouteMetadata metadata)
        {
            if (!this.verbs.TryGetValue(metadata.Verb, out Routes parent))
            {
                parent = new Routes();
                this.verbs.Add(metadata.Verb, parent);
            }

            RouteNode<Versions> node = parent.Root.Add(matches, 0);
            if (node.Value == null)
            {
                node.Value = new Versions();
            }

            node.Value.Add(target, metadata.MinimumVersion, metadata.MaximumVersion);
        }
    }
}
