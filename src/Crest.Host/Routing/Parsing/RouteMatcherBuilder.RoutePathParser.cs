// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Crest.Core;
    using Crest.Host.Routing.Captures;
    using static System.Diagnostics.Debug;

    /// <content>
    /// Contains the nested <see cref="RoutePathParser"/> class.
    /// </content>
    internal partial class RouteMatcherBuilder
    {
        private sealed class RoutePathParser : UrlParser
        {
            private readonly List<IMatchNode> nodes = new List<IMatchNode>();
            private readonly IReadOnlyDictionary<Type, Func<string, IMatchNode>> knownMatchers;
            private readonly List<QueryCapture> queryCaptures = new List<QueryCapture>();
            private string queryCatchAll;

            internal RoutePathParser(
                bool canReadBody,
                IReadOnlyDictionary<Type, Func<string, IMatchNode>> knownMatchers)
                : base(canReadBody)
            {
                this.knownMatchers = knownMatchers;
            }

            public (string name, Type type) BodyParameter { get; private set; }

            public IReadOnlyList<IMatchNode> Nodes => this.nodes;

            internal QueryCapture[] GetQueryCaptures()
            {
                // It's important we add this *after* all the other query
                // captures have had change to capture their values
                if (this.queryCatchAll != null)
                {
                    this.queryCaptures.Add(QueryCapture.CreateCatchAll(this.queryCatchAll));
                }

                return this.queryCaptures.ToArray();
            }

            internal void ParseUrl(string routeUrl, IReadOnlyCollection<ParameterInfo> parameters)
            {
                ParameterData ConvertParameter(ParameterInfo info)
                {
                    return new ParameterData
                    {
                        HasBodyAttribute = info.GetCustomAttribute(typeof(FromBodyAttribute)) != null,
                        IsOptional = info.IsOptional,
                        Name = info.Name,
                        ParameterType = info.ParameterType,
                    };
                }

                if (!routeUrl.StartsWith("/", StringComparison.Ordinal))
                {
                    routeUrl = "/" + routeUrl;
                }

                this.ParseUrl(routeUrl, parameters.Select(ConvertParameter));
            }

            protected override void OnCaptureBody(Type parameterType, string name)
            {
                this.BodyParameter = (name, parameterType);
            }

            protected override void OnCaptureParameter(Type parameterType, string name)
            {
                if (this.knownMatchers.TryGetValue(parameterType, out Func<string, IMatchNode> factoryMethod))
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

            protected override void OnQueryCatchAll(string name)
            {
                this.queryCatchAll = name;
            }

            protected override void OnQueryParameter(string name, Type parameterType)
            {
                // Helper method to avoid QueryCaptures taking a dependency on
                // IMatchNode when all it needs is an IQueryValueConverter that
                // IMatchNode inherits from
                bool TryGetConverter(Type type, out Func<string, IQueryValueConverter> value)
                {
                    bool result = this.knownMatchers.TryGetValue(
                        type,
                        out Func<string, IMatchNode> node);

                    value = node;
                    return result;
                }

                this.queryCaptures.Add(
                    QueryCapture.Create(name, parameterType, TryGetConverter));
            }

            private static string GetErrorMessage(ErrorType error, string value)
            {
                switch (error)
                {
                    case ErrorType.CannotBeMarkedAsFromBody:
                        return "FromBody parameters cannot be captured";

                    case ErrorType.DuplicateParameter:
                        return "Parameter is captured multiple times";

                    case ErrorType.IncorrectCatchAllType:
                        return "Catch-all parameter must be an object/dynamic type";

                    case ErrorType.MissingClosingBrace:
                        return "Missing closing brace";

                    case ErrorType.MultipleBodyParameters:
                        return "Cannot have multiple body parameters";

                    case ErrorType.MultipleCatchAllParameters:
                        return "Cannot have multiple query catch-all parameters";

                    case ErrorType.MustBeOptional:
                        return "Query parameters must be optional";

                    case ErrorType.ParameterNotFound:
                        return "Parameter is missing from the URL";

                    default:
                        Assert(error == ErrorType.UnknownParameter, "Unknown ErrorType value");
                        return "Unable to find parameter called: " + value;
                }
            }
        }
    }
}
