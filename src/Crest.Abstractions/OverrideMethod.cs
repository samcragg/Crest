// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System.Threading.Tasks;

    /// <summary>
    /// Allows direct processing of a request without going through the
    /// normal routing pipeline.
    /// </summary>
    /// <param name="request">Contains the request data.</param>
    /// <param name="converter">
    /// Can be used to convert an object into the requested format.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The value of the
    /// <c>TResult</c> parameter contains the response data.
    /// </returns>
    public delegate Task<IResponseData> OverrideMethod(IRequestData request, IContentConverter converter);
}
