// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Crest.Host.Conversion;

    /// <summary>
    /// Generates responses for various status codes.
    /// </summary>
    /// <remarks>
    /// The class is unsealed and the methods are virtual to allow faking in
    /// unit tests.
    /// </remarks>
    internal class ResponseGenerator
    {
        private static readonly ResponseData NoContent =
            new ResponseData(string.Empty, (int)HttpStatusCode.NoContent);

        private static readonly ResponseData NotAcceptable =
            new ResponseData(string.Empty, (int)HttpStatusCode.NotAcceptable);

        private readonly StatusCodeHandler[] handlers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseGenerator"/> class.
        /// </summary>
        /// <param name="handlers">The handlers to invoke.</param>
        public ResponseGenerator(IEnumerable<StatusCodeHandler> handlers)
        {
            this.handlers = handlers.ToArray();
            Array.Sort(this.handlers, (a, b) => a.Order.CompareTo(b.Order));
        }

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
        public virtual Task<IResponseData> NoContentAsync(IRequestData request, IContentConverter converter)
        {
            return this.FindResponse(
                h => h.NoContentAsync(request, converter),
                NoContent);
        }

        /// <summary>
        /// Generates a response for 406 Not Acceptable.
        /// </summary>
        /// <param name="request">The request to reply to.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter contains the response to send.
        /// </returns>
        public virtual Task<IResponseData> NotAcceptableAsync(IRequestData request)
        {
            return this.FindResponse(
                h => h.NotAcceptableAsync(request),
                NotAcceptable);
        }

        private async Task<IResponseData> FindResponse(
            Func<StatusCodeHandler, Task<IResponseData>> method,
            IResponseData defaultResponse)
        {
            for (int i = 0; i < this.handlers.Length; i++)
            {
                IResponseData response = await method(this.handlers[i]).ConfigureAwait(false);
                if (response != null)
                {
                    return response;
                }
            }

            return defaultResponse;
        }
    }
}
