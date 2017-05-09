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
        private sealed class NodeParser : UrlParser
        {
            private readonly Dictionary<Type, Func<string, IMatchNode>> specializedCaptureNodes;

            internal NodeParser(Dictionary<Type, Func<string, IMatchNode>> specializedCaptureNodes)
            {
                this.specializedCaptureNodes = specializedCaptureNodes;
            }

            internal List<IMatchNode> Nodes { get; } = new List<IMatchNode>();

            protected override void OnCaptureSegment(Type parameterType, string name)
            {
                if (this.specializedCaptureNodes.TryGetValue(
                        parameterType,
                        out Func<string, IMatchNode> factoryMethod))
                {
                    this.Nodes.Add(factoryMethod(name));
                }
                else
                {
                    this.Nodes.Add(new GenericCaptureNode(name, parameterType));
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
                this.Nodes.Add(new LiteralNode(value));
            }

            protected override void OnQueryParameter(string key, Type parameterType, string name)
            {
                throw new NotImplementedException();
            }
        }
    }
}
