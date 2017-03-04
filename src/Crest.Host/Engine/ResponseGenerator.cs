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
    internal sealed class ResponseGenerator : IResponseStatusGenerator
    {
        /// <summary>
        /// Represents a response that indicates there was an internal server
        /// error whilst dealing with the request.
        /// </summary>
        internal static readonly ResponseData InternalError =
            new ResponseData(string.Empty, (int)HttpStatusCode.InternalServerError);

        private static readonly ResponseData NoContent =
            new ResponseData(string.Empty, (int)HttpStatusCode.NoContent);

        private static readonly ResponseData NotAcceptable =
            new ResponseData(string.Empty, (int)HttpStatusCode.NotAcceptable);

        private static readonly ResponseData NotFound =
            new ResponseData(string.Empty, (int)HttpStatusCode.NotFound);

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

        /// <inheritdoc />
        public Task<IResponseData> InternalErrorAsync(Exception exception)
        {
            return this.FindResponse(
                h => h.InternalErrorAsync(exception),
                InternalError);
        }

        /// <inheritdoc />
        public Task<IResponseData> NoContentAsync(IRequestData request, IContentConverter converter)
        {
            return this.FindResponse(
                h => h.NoContentAsync(request, converter),
                NoContent);
        }

        /// <inheritdoc />
        public Task<IResponseData> NotAcceptableAsync(IRequestData request)
        {
            return this.FindResponse(
                h => h.NotAcceptableAsync(request),
                NotAcceptable);
        }

        /// <inheritdoc />
        public Task<IResponseData> NotFoundAsync(IRequestData request, IContentConverter converter)
        {
            return this.FindResponse(
                h => h.NotFoundAsync(request, converter),
                NotFound);
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
