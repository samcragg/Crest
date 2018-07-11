// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Crest.Abstractions;

    /// <summary>
    /// Contains the response to a request.
    /// </summary>
    internal sealed class ResponseData : IResponseData
    {
        private readonly StringDictionary<string> headers = new StringDictionary<string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseData"/> class.
        /// </summary>
        /// <param name="content">The content type.</param>
        /// <param name="code">The HTTP status code.</param>
        /// <param name="body">Used to write the response body.</param>
        public ResponseData(string content, int code, Func<Stream, Task> body = null)
        {
            this.ContentType = content;
            this.StatusCode = code;
            this.WriteBody = body ?? (_ => Task.CompletedTask);
        }

        /// <inheritdoc />
        public string ContentType { get; }

        /// <inheritdoc />
        public int StatusCode { get; }

        /// <inheritdoc />
        public Func<Stream, Task> WriteBody { get; }

        /// <inheritdoc />
        IReadOnlyDictionary<string, string> IResponseData.Headers => this.headers;

        /// <summary>
        /// Gets the headers to send with the response.
        /// </summary>
        internal IDictionary<string, string> Headers => this.headers;
    }
}
