// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System.Threading.Tasks;
    using Crest.Host.Conversion;

    /// <summary>
    /// Allows the interception of specific status codes when sending a reply
    /// to a request.
    /// </summary>
    public abstract class StatusCodeHandler
    {
        private static readonly Task<IResponseData> NoResponse =
            Task.FromResult<IResponseData>(null);

        /// <summary>
        /// Gets the relative order this should be invoked in.
        /// </summary>
        /// <remarks>
        /// Lower numbers will be invoked before higher numbers (i.e. 100 will
        /// be executed before 200).
        /// </remarks>
        public abstract int Order { get; }

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
            return NoResponse;
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
            return NoResponse;
        }
    }
}
