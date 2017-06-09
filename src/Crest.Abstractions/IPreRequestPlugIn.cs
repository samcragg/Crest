// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System.Threading.Tasks;

    /// <summary>
    /// Allows inspection of a request before it has been passed to the
    /// registered route handler, allowing the request to be terminated early
    /// without any further processing.
    /// </summary>
    public interface IPreRequestPlugin
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
        /// Performs custom processing of the request.
        /// </summary>
        /// <param name="request">The request data.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter may contain the response to send. If this
        /// is null then the request is allowed to be processed further.
        /// </returns>
        /// <remarks>
        /// Return a task with a null result to allow the request to be
        /// processed in the normal way.
        /// </remarks>
        Task<IResponseData> ProcessAsync(IRequestData request);
    }
}
