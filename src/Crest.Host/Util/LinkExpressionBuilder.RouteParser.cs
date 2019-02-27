// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Util
{
    using System;
    using System.Reflection;
    using Crest.Host.Routing.Parsing;

    /// <content>
    /// Contains the nested <see cref="RouteParser"/> class.
    /// </content>
    internal partial class LinkExpressionBuilder
    {
        private class RouteParser : UrlParser
        {
            private readonly DelegateBuilder builder;
            private readonly ParameterData[] parameterData;

            internal RouteParser(DelegateBuilder builder, ParameterInfo[] parameters)
                : base(canReadBody: true)
            {
                this.builder = builder;

                // Put them in the order they are passed into the method so when
                // we find a parameter later we know it's position by it's index
                this.parameterData = new ParameterData[parameters.Length];
                foreach (ParameterInfo info in parameters)
                {
                    this.parameterData[info.Position] = new ParameterData(info);
                }
            }

            internal void Parse(string url)
            {
                this.ParseUrl(url, this.parameterData);
            }

            protected override void OnCaptureBody(Type parameterType, string name)
            {
            }

            protected override void OnCaptureParameter(Type parameterType, string name)
            {
                this.builder.AddCapture(parameterType, this.GetParameterIndex(name));
            }

            protected override void OnError(ErrorType error, string parameter)
            {
                throw new InvalidOperationException("Error parsing route: " + error);
            }

            protected override void OnError(ErrorType error, int start, int length, string value)
            {
                throw new InvalidOperationException("Error parsing route: " + error);
            }

            protected override void OnLiteralSegment(string value)
            {
                this.builder.AddLiteral(value);
            }

            protected override void OnQueryCatchAll(string name)
            {
                this.builder.AddQuery(null, typeof(object), this.GetParameterIndex(name));
            }

            protected override void OnQueryParameter(string name, Type parameterType)
            {
                this.builder.AddQuery(name, parameterType, this.GetParameterIndex(name));
            }

            private int GetParameterIndex(string name)
            {
                return Array.FindIndex(
                    this.parameterData,
                    p => string.Equals(name, p.Name, StringComparison.Ordinal));
            }
        }
    }
}
