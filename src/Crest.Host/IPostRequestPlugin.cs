// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System.Threading.Tasks;

    /// <summary>
    /// Allows inspection of a request after it has been processed by the
    /// registered route handler.
    /// </summary>
    public interface IPostRequestPlugin
    {
        /// <summary>
        /// Gets the relative order this should be invoked in.
        /// </summary>
        /// <remarks>
        /// Lower numbers will be invoked before higher numbers (i.e. 100 will
        /// be executed before 200).
        /// </remarks>
        int Order { get; }

        /// <summary>
        /// Performs custom processing of the response to a request.
        /// </summary>
        /// <param name="request">The request data.</param>
        /// <param name="response">The generator response data.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter contains the response to send.
        /// </returns>
        Task<IResponseData> ProcessAsync(IRequestData request, IResponseData response);
    }
}
