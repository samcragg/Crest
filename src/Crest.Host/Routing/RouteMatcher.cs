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
    using Crest.Abstractions;
    using Crest.Host.Routing.Captures;

    /// <summary>
    /// Allows the matching of routes to their handler.
    /// </summary>
    internal sealed class RouteMatcher : IRouteMapper
    {
        // The method is stored against its MetadataToken so we can find it again
        private readonly IReadOnlyDictionary<int, RouteMethod> adapters;
        private readonly ILookup<string, EndpointInfo<OverrideMethod>> overrides;
        private readonly RouteTrie<EndpointInfo<RouteMethodInfo>> routes;

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteMatcher"/> class.
        /// </summary>
        /// <param name="methods">The mapped methods.</param>
        /// <param name="routes">The routes to match.</param>
        /// <param name="overrides">The override routes.</param>
        public RouteMatcher(
            IReadOnlyCollection<RouteMethod> methods,
            RouteTrie<EndpointInfo<RouteMethodInfo>> routes,
            ILookup<string, EndpointInfo<OverrideMethod>> overrides)
        {
            var dictionary = new Dictionary<int, RouteMethod>(methods.Count);
            foreach (RouteMethod method in methods)
            {
                // Allow multiple routes to the same method
                dictionary[method.Method.MetadataToken] = method;
            }

            this.adapters = dictionary;
            this.overrides = overrides;
            this.routes = routes;
        }

        /// <inheritdoc />
        public OverrideMethod FindOverride(string verb, string path)
        {
            foreach (EndpointInfo<OverrideMethod> endpoint in this.overrides[path])
            {
                if (IsApplicable(endpoint, verb, 0))
                {
                    return endpoint.Value;
                }
            }

            return null;
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
            return this.adapters.Values.Select(r => r.Method);
        }

        /// <inheritdoc />
        public RouteMapperMatchResult Match(string verb, string path, ILookup<string, string> query)
        {
            RouteTrie<EndpointInfo<RouteMethodInfo>>.MatchResult match =
                this.routes.Match(path.AsSpan());

            if (match.Success)
            {
                match.Captures.TryGetValue(VersionCaptureNode.KeyName, out object capturedVersion);
                int version = capturedVersion == null ? 0 : (int)capturedVersion;

                foreach (EndpointInfo<RouteMethodInfo> value in match.Values)
                {
                    if (IsApplicable(value, verb, version))
                    {
                        var captures = (IDictionary<string, object>)match.Captures;
                        AddPlaceholders(value.Value, captures);
                        SaveQueryParameters(query, value.Value, captures);
                        return new RouteMapperMatchResult(
                            value.Value.Method,
                            match.Captures);
                    }
                }
            }

            return null;
        }

        private static void AddPlaceholders(RouteMethodInfo method, IDictionary<string, object> captures)
        {
            // We need to store this in the parameters now so we can update it
            // in the RequestProcessor with a scoped service provider
            captures.Add(ServiceProviderPlaceholder.Key, new ServiceProviderPlaceholder());

            if (method.HasBodyParameter)
            {
                captures.Add(
                    method.BodyParameterName,
                    new RequestBodyPlaceholder(method.BodyType));
            }
        }

        private static bool IsApplicable<T>(EndpointInfo<T> endpoint, string verb, int version)
        {
            if (endpoint.Verb.Equals(verb, StringComparison.OrdinalIgnoreCase))
            {
                return endpoint.Matches(version);
            }
            else
            {
                return false;
            }
        }

        private static void SaveQueryParameters(
            ILookup<string, string> query,
            RouteMethodInfo method,
            IDictionary<string, object> captures)
        {
            foreach (QueryCapture capture in method.QueryCaptures)
            {
                capture.ParseParameters(query, captures);
            }
        }
    }
}
