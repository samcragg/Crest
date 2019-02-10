// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Crest.Host.Routing.Captures;

    /// <content>
    /// Contains the nested <see cref="Target"/> struct.
    /// </content>
    internal sealed partial class RouteMapper
    {
        private struct Target
        {
            public Target(MethodInfo method, NodeBuilder.IParseResult result)
            {
                IReadOnlyList<QueryCapture> captures = result.QueryCaptures;

                this.BodyParameter = result.BodyParameter.name;
                this.BodyType = result.BodyParameter.type;
                this.Method = method;
                this.QueryCaptures = (captures.Count > 0) ? captures : null;
            }

            public string BodyParameter { get; }

            public Type BodyType { get; }

            public bool HasBodyParameter => this.BodyParameter != null;

            public MethodInfo Method { get; }

            public IReadOnlyList<QueryCapture> QueryCaptures { get; }
        }
    }
}
