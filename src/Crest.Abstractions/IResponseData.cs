// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Contains information about the response to a request.
    /// </summary>
    public interface IResponseData
    {
        /// <summary>
        /// Gets the content type of the returned data.
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Gets addtional headers to send with the response.
        /// </summary>
        IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        int StatusCode { get; }

        /// <summary>
        /// Gets a method to call that, when passed a <see cref="Stream"/>,
        /// writes the response body to it asynchronously.
        /// </summary>
        Func<Stream, Task> WriteBody { get; }
    }
}
