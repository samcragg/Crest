// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System.Net;

    /// <summary>
    /// Contains the response to a request.
    /// </summary>
    internal sealed class ResponseData : IResponseData
    {
        /// <summary>
        /// Represents an empty No Content (204) response.
        /// </summary>
        internal static readonly ResponseData NoContent =
            new ResponseData(string.Empty, (int)HttpStatusCode.NoContent);

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseData"/> class.
        /// </summary>
        /// <param name="content">The content type.</param>
        /// <param name="code">The HTTP status code.</param>
        public ResponseData(string content, int code)
        {
            this.ContentType = content;
            this.StatusCode = code;
        }

        /// <inheritdoc />
        public string ContentType
        {
            get;
        }

        /// <inheritdoc />
        public int StatusCode
        {
            get;
        }
    }
}
