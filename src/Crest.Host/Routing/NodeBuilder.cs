// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using Crest.Abstractions;

    /// <summary>
    /// Allows the creation of nodes used to match a route whilst checking for
    /// ambiguous matches.
    /// </summary>
    internal sealed partial class NodeBuilder
    {
        private readonly HashSet<string> normalizedUrls =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<Type, Func<string, IMatchNode>> specializedCaptureNodes =
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

        /// <summary>
        /// Parses the specified route into a sequence of nodes.
        /// </summary>
        /// <param name="route">The route information.</param>
        /// <returns>The parsed nodes.</returns>
        public IParseResult Parse(RouteMetadata route)
        {
            var parser = new NodeParser(route.CanReadBody, this.specializedCaptureNodes);
            parser.ParseUrl(route.Path, route.Method.GetParameters());

            string normalizedUrl = GetNormalizedRoute(route, parser.Nodes);
            if (!this.normalizedUrls.Add(normalizedUrl))
            {
                throw new InvalidOperationException("The route produces an ambiguous match.");
            }

            return parser;
        }

        private static void AppendNodeString(StringBuilder buffer, IMatchNode node)
        {
            if (node is LiteralNode literal)
            {
                buffer.Append(literal.Literal.ToUpperInvariant());
            }
            else
            {
                // We allow multiple captures as long as they don't have the
                // same priority (i.e. {100} and {200} are OK as we'll try to
                // match the 200 first)
                buffer.Append('{')
                      .Append(node.Priority)
                      .Append('}');
            }
        }

        private static void AppendVerbAndVersion(StringBuilder buffer, RouteMetadata metadata)
        {
            buffer.Append(metadata.Verb)
                  .Append(':')
                  .Append(metadata.MinimumVersion.ToString(CultureInfo.InvariantCulture))
                  .Append(':')
                  .Append(metadata.MaximumVersion.ToString(CultureInfo.InvariantCulture));
        }

        private static string GetNormalizedRoute(RouteMetadata route, IEnumerable<IMatchNode> nodes)
        {
            var builder = new StringBuilder(route.Path.Length * 2);
            AppendVerbAndVersion(builder, route);

            foreach (IMatchNode node in nodes)
            {
                builder.Append('/');
                AppendNodeString(builder, node);
            }

            return builder.ToString();
        }
    }
}
