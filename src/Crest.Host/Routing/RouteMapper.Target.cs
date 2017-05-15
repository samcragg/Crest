// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System.Collections.Generic;
    using System.Reflection;

    /// <content>
    /// Contains the nested <see cref="Target"/> struct.
    /// </content>
    internal sealed partial class RouteMapper
    {
        private struct Target
        {
            public Target(MethodInfo method, IReadOnlyList<QueryCapture> captures)
            {
                this.Method = method;
                this.QueryCaptures = (captures.Count > 0) ? captures : null;
            }

            public MethodInfo Method { get; }

            public IReadOnlyList<QueryCapture> QueryCaptures { get; }
        }
    }
}
