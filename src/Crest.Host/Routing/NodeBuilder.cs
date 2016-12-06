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
    using System.Text;

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
        /// <param name="version">The version information.</param>
        /// <param name="routeUrl">The route URL to add.</param>
        /// <param name="parameters">The parameters to capture.</param>
        /// <returns>The parsed nodes.</returns>
        public IMatchNode[] Parse(string version, string routeUrl, IEnumerable<ParameterInfo> parameters)
        {
            IReadOnlyDictionary<string, Type> parameterPairs =
                parameters.ToDictionary(p => p.Name, p => p.ParameterType, StringComparer.Ordinal);

            var parser = new NodeParser(this.specializedCaptureNodes);
            parser.ParseUrl(routeUrl, parameterPairs);

            string normalizedUrl = GetNormalizedRoute(version, routeUrl, parser.Nodes);
            if (!this.normalizedUrls.Add(normalizedUrl))
            {
                throw new InvalidOperationException("The route produces an ambiguous match.");
            }

            return parser.Nodes.ToArray();
        }

        private static void AppendNodeString(StringBuilder buffer, IMatchNode node)
        {
            var literal = node as LiteralNode;
            if (literal != null)
            {
                buffer.Append(literal.Literal.ToLowerInvariant());
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

        private static string GetNormalizedRoute(string versionInfo, string routeUrl, IEnumerable<IMatchNode> nodes)
        {
            var builder = new StringBuilder(routeUrl.Length * 2);
            builder.Append(versionInfo);

            foreach (IMatchNode node in nodes)
            {
                builder.Append('/');
                AppendNodeString(builder, node);
            }

            return builder.ToString();
        }
    }
}
