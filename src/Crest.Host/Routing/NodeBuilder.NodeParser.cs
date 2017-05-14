// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;

    /// <content>
    /// Contains the nested <see cref="NodeParser"/> struct.
    /// </content>
    internal sealed partial class NodeBuilder
    {
        private sealed class NodeParser : UrlParser, IParseResult
        {
            private readonly List<IMatchNode> nodes = new List<IMatchNode>();
            private readonly List<QueryCapture> queryCaptures = new List<QueryCapture>();
            private readonly IReadOnlyDictionary<Type, Func<string, IMatchNode>> specializedCaptureNodes;

            internal NodeParser(IReadOnlyDictionary<Type, Func<string, IMatchNode>> specializedCaptureNodes)
            {
                this.specializedCaptureNodes = specializedCaptureNodes;
            }

            public IReadOnlyList<IMatchNode> Nodes => this.nodes;

            public IReadOnlyList<QueryCapture> QueryCaptures => this.queryCaptures;

            protected override void OnCaptureSegment(Type parameterType, string name)
            {
                if (this.specializedCaptureNodes.TryGetValue(
                        parameterType,
                        out Func<string, IMatchNode> factoryMethod))
                {
                    this.nodes.Add(factoryMethod(name));
                }
                else
                {
                    this.nodes.Add(new GenericCaptureNode(name, parameterType));
                }
            }

            protected override void OnError(string error, string parameter)
            {
                throw new FormatException(error);
            }

            protected override void OnError(string error, int start, int length)
            {
                throw new FormatException(error);
            }

            protected override void OnLiteralSegment(string value)
            {
                this.nodes.Add(new LiteralNode(value));
            }

            protected override void OnQueryParameter(string key, Type parameterType, string name)
            {
                // Helper method to avoid QueryCaptures taking a dependency on
                // IMatchNode when all it needs is an IQueryValueConverter that
                // IMatchNode inherits from
                bool TryGetConverter(Type type, out Func<string, IQueryValueConverter> value)
                {
                    bool result = this.specializedCaptureNodes.TryGetValue(
                        type,
                        out Func<string, IMatchNode> node);

                    value = node;
                    return result;
                }

                this.queryCaptures.Add(
                    QueryCapture.Create(key, parameterType, name, TryGetConverter));
            }
        }
    }
}
