// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Contains information about the incoming request.
    /// </summary>
    public interface IRequestData
    {
        /// <summary>
        /// Gets the body sent with the request.
        /// </summary>
        Stream Body { get; }

        /// <summary>
        /// Gets the method to be invoked that handles the matched route.
        /// </summary>
        MethodInfo Handler { get; }

        /// <summary>
        /// Gets the headers of the request.
        /// </summary>
        /// <remarks>
        /// When implementing the interface, multiple headers of the same field
        /// value MUST be combined into a single comma separator header value,
        /// per <a href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec4.html#sec4.2">RFC 2616</a>:
        /// &quot;It MUST be possible to combine the multiple header fields
        /// into one &quot;field-name: field-value&quot; pair, without changing
        /// the semantics of the message&quot;.
        /// </remarks>
        IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets the parameters that will be passed to the handler.
        /// </summary>
        IReadOnlyDictionary<string, object> Parameters { get; }

        /// <summary>
        /// Gets the requested URL.
        /// </summary>
        Uri Url { get; }
    }
}
