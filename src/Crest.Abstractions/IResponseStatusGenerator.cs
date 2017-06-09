// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Generates responses for various status codes.
    /// </summary>
    public interface IResponseStatusGenerator
    {
        /// <summary>
        /// Generates a response for 500 Internal Server Error.
        /// </summary>
        /// <param name="exception">The exception that caused the internal error.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter contains the response to send.
        /// </returns>
        Task<IResponseData> InternalErrorAsync(Exception exception);

        /// <summary>
        /// Generates a response for 204 No Content.
        /// </summary>
        /// <param name="request">The request to reply to.</param>
        /// <param name="converter">
        /// Allows the conversion to the requested content type.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter contains the response to send.
        /// </returns>
        Task<IResponseData> NoContentAsync(IRequestData request, IContentConverter converter);

        /// <summary>
        /// Generates a response for 406 Not Acceptable.
        /// </summary>
        /// <param name="request">The request to reply to.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter contains the response to send.
        /// </returns>
        Task<IResponseData> NotAcceptableAsync(IRequestData request);

        /// <summary>
        /// Generates a response for 404 Not Found.
        /// </summary>
        /// <param name="request">The request to reply to.</param>
        /// <param name="converter">
        /// Allows the conversion to the requested content type.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter contains the response to send.
        /// </returns>
        Task<IResponseData> NotFoundAsync(IRequestData request, IContentConverter converter);
    }
}
