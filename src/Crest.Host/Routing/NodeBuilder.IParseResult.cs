// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;

    /// <content>
    /// Contains the nested <see cref="IParseResult"/> interface.
    /// </content>
    internal sealed partial class NodeBuilder
    {
        /// <summary>
        /// Contains information from parsing a route URL.
        /// </summary>
        internal interface IParseResult
        {
            /// <summary>
            /// Gets the parameter that the request body is injected into.
            /// </summary>
            KeyValuePair<string, Type>? BodyParameter { get; }

            /// <summary>
            /// Gets the list of node that were parsed.
            /// </summary>
            IReadOnlyList<IMatchNode> Nodes { get; }

            /// <summary>
            /// Gets the list query values that were parsed.
            /// </summary>
            IReadOnlyList<QueryCapture> QueryCaptures { get; }
        }
    }
}
