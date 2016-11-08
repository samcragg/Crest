// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
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
        /// Gets the HTTP status code.
        /// </summary>
        int StatusCode { get; }
    }
}
