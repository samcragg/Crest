// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Abstractions;
    using Crest.Host.Routing.Captures;

    /// <summary>
    /// Allows the building of <see cref="RouteMatcher"/>s.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Standard dictates we should normalize to lowercase")]
    internal partial class RouteMatcherBuilder
    {
        private readonly RouteMethodAdapter methodAdapter;
        private readonly List<RouteMethod> methods = new List<RouteMethod>();

        private readonly List<(string path, EndpointInfo<OverrideMethod> endpoint)> overrides =
            new List<(string path, EndpointInfo<OverrideMethod> endpoint)>();

        private readonly Dictionary<Type, Func<string, IMatchNode>> specializedCaptures =
            new Dictionary<Type, Func<string, IMatchNode>>
            {
                { typeof(bool), n => new BoolCaptureNode(n) },
                { typeof(byte), n => new IntegerCaptureNode(n, typeof(byte)) },
                { typeof(Guid), n => new GuidCaptureNode(n) },
                { typeof(int), n => new IntegerCaptureNode(n, typeof(int)) },
                { typeof(long), n => new IntegerCaptureNode(n, typeof(long)) },
                { typeof(sbyte), n => new IntegerCaptureNode(n, typeof(sbyte)) },
                { typeof(short), n => new IntegerCaptureNode(n, typeof(short)) },
                { typeof(string), n => new StringCaptureNode(n) },
                { typeof(uint), n => new IntegerCaptureNode(n, typeof(uint)) },
                { typeof(ulong), n => new IntegerCaptureNode(n, typeof(ulong)) },
                { typeof(ushort), n => new IntegerCaptureNode(n, typeof(ushort)) },
            };

        private readonly RouteTrieBuilder<EndpointInfo<RouteMethodInfo>> trieBuilder =
            new RouteTrieBuilder<EndpointInfo<RouteMethodInfo>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteMatcherBuilder"/> class.
        /// </summary>
        public RouteMatcherBuilder()
            : this(new RouteMethodAdapter())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RouteMatcherBuilder"/> class.
        /// </summary>
        /// <param name="methodAdapter">Used to create the <see cref="RouteMethod"/>s.</param>
        protected RouteMatcherBuilder(RouteMethodAdapter methodAdapter)
        {
            this.methodAdapter = methodAdapter;
        }

        /// <summary>
        /// Adds the specific method.
        /// </summary>
        /// <param name="metadata">Represents information about the route method.</param>
        public void AddMethod(RouteMetadata metadata)
        {
            var parser = new RoutePathParser(metadata.CanReadBody, this.specializedCaptures);
            parser.ParseUrl(metadata.Path, metadata.Method.GetParameters());

            var routeMethod = new RouteMethodInfo(
                parser.BodyParameter.name,
                parser.BodyParameter.type,
                metadata.Method,
                parser.GetQueryCaptures());

            var endpoint = new EndpointInfo<RouteMethodInfo>(
                metadata.Verb,
                routeMethod,
                metadata.MinimumVersion,
                metadata.MaximumVersion);

            RouteMethod lambda = this.methodAdapter.CreateMethod(
                metadata.Factory,
                metadata.Method);

            this.trieBuilder.Add(parser.Nodes, endpoint);
            this.methods.Add(lambda);
        }

        /// <summary>
        /// Adds the specific method as an override.
        /// </summary>
        /// <param name="verb">The HTTP verb to match on.</param>
        /// <param name="path">The full path to match.</param>
        /// <param name="method">The callback to invoke on a successful match.</param>
        public void AddOverride(string verb, string path, OverrideMethod method)
        {
            path = path.ToLowerInvariant();
            verb = verb.ToUpperInvariant();
            foreach ((string p, EndpointInfo<OverrideMethod> e) in this.overrides)
            {
                if (string.Equals(path, p, StringComparison.Ordinal))
                {
                    VerifyEndpointIsDifferent(e, path, verb);
                }
            }

            this.overrides.Add((path, new EndpointInfo<OverrideMethod>(verb, method, 0, 0)));
        }

        /// <summary>
        /// Create a new <see cref="RouteMatcher"/> from the information added
        /// to this instance.
        /// </summary>
        /// <returns>A new instance that represents the state of this class.</returns>
        public RouteMatcher Build()
        {
            return this.CreateMatcher(
                this.methods,
                this.trieBuilder.Build(),
                this.BuildOverrides());
        }

        /// <summary>
        /// Allows the creation of a <see cref="RouteMatcher"/>.
        /// </summary>
        /// <param name="methods">The mapped methods.</param>
        /// <param name="routes">The routes to match.</param>
        /// <param name="overrides">The override routes.</param>
        /// <returns>A new instance of the class.</returns>
        /// <remarks>
        /// This method is to allow for unit testing of how the matcher gets
        /// constructed.
        /// </remarks>
        protected virtual RouteMatcher CreateMatcher(
            IReadOnlyCollection<RouteMethod> methods,
            RouteTrie<EndpointInfo<RouteMethodInfo>> routes,
            ILookup<string, EndpointInfo<OverrideMethod>> overrides)
        {
            return new RouteMatcher(methods, routes, overrides);
        }

        private static void VerifyEndpointIsDifferent(
            EndpointInfo<OverrideMethod> endpoint,
            string path,
            string verb)
        {
            if (string.Equals(endpoint.Verb, verb, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Ambiguous route override for '" + path + "'");
            }
        }

        private ILookup<string, EndpointInfo<OverrideMethod>> BuildOverrides()
        {
            return this.overrides.ToLookup(x => x.path, x => x.endpoint);
        }
    }
}
