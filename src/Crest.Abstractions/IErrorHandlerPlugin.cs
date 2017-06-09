// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows custom error handling logic to be applied.
    /// </summary>
    public interface IErrorHandlerPlugin
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
        /// Determines whether this instance can handle the specified exception.
        /// </summary>
        /// <param name="exception">The exception that has been raised.</param>
        /// <returns>
        /// <c>true</c> if <see cref="ProcessAsync(IRequestData, Exception)"/> should be invoked
        /// on this instance; otherwise, <c>false</c>.
        /// </returns>
        bool CanHandle(Exception exception);

        /// <summary>
        /// Performs custom processing of the response to a request.
        /// </summary>
        /// <param name="request">The request data.</param>
        /// <param name="exception">The exception that has been raised.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter contains the response to send.
        /// </returns>
        Task<IResponseData> ProcessAsync(IRequestData request, Exception exception);
    }
}
