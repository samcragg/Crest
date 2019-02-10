// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;
    using Crest.Host.Routing.Captures;

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
            (string name, Type type) BodyParameter { get; }

            /// <summary>
            /// Gets the list of node that were parsed.
            /// </summary>
            IReadOnlyList<IMatchNode> Nodes { get; }

            /// <summary>
            /// Gets the list query values that were parsed.
            /// </summary>
            IReadOnlyList<QueryCapture> QueryCaptures { get; }

            /// <summary>
            /// Gets the name of the parameter used to catch any extra query
            /// key values not captured.
            /// </summary>
            string QueryCatchAll { get; }
        }
    }
}
