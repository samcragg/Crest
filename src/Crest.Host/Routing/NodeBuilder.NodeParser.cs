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
    using Crest.Core;
    using static System.Diagnostics.Debug;

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

            internal NodeParser(
                bool canReadBody,
                IReadOnlyDictionary<Type, Func<string, IMatchNode>> specializedCaptureNodes)
                : base(canReadBody)
            {
                this.specializedCaptureNodes = specializedCaptureNodes;
            }

            public KeyValuePair<string, Type>? BodyParameter { get; private set; }

            public IReadOnlyList<IMatchNode> Nodes => this.nodes;

            public IReadOnlyList<QueryCapture> QueryCaptures => this.queryCaptures;

            internal void ParseUrl(string routeUrl, IReadOnlyCollection<ParameterInfo> parameters)
            {
                ParameterData ConvertParameter(ParameterInfo info)
                {
                    return new ParameterData
                    {
                        HasBodyAttribute = info.GetCustomAttribute(typeof(FromBodyAttribute)) != null,
                        IsOptional = info.IsOptional,
                        Name = info.Name,
                        ParameterType = info.ParameterType
                    };
                }

                this.ParseUrl(routeUrl, parameters.Select(ConvertParameter));
            }

            protected override void OnCaptureBody(Type parameterType, string name)
            {
                this.BodyParameter = new KeyValuePair<string, Type>(name, parameterType);
            }

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

            protected override void OnError(ErrorType error, string parameter)
            {
                throw new FormatException(GetErrorMessage(error, parameter));
            }

            protected override void OnError(ErrorType error, int start, int length, string value)
            {
                throw new FormatException(GetErrorMessage(error, value));
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

            private static string GetErrorMessage(ErrorType error, string value)
            {
                switch (error)
                {
                    case ErrorType.DuplicateParameter:
                        return "Parameter is captured multiple times";

                    case ErrorType.MissingClosingBrace:
                        return "Missing closing brace";

                    case ErrorType.MissingQueryValue:
                        return "Missing query value capture";

                    case ErrorType.MustBeOptional:
                        return "Query parameters must be optional";

                    case ErrorType.MustCaptureQueryValue:
                        return "Query values must be parameter captures";

                    case ErrorType.ParameterNotFound:
                        return "Parameter is missing from the URL";

                    case ErrorType.UnescapedBrace:
                        return "Unescaped braces are not allowed";

                    default:
                        Assert(error == ErrorType.UnknownParameter, "Unknown enum value");
                        return "Unable to find parameter called: " + value;
                }
            }
        }
    }
}
