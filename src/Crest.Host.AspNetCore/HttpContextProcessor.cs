// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.AspNetCore
{
    using System.Threading.Tasks;
    using Crest.Host;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Allows the orchestration of the handling of a request.
    /// </summary>
    internal sealed class HttpContextProcessor : RequestProcessor
    {
        /// <summary>
        /// Handles the request, adapting the ASP.NET Core objects to enable
        /// processing by the Crest framework.
        /// </summary>
        /// <param name="context">The HTTP request.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task HandleRequest(HttpContext context)
        {
            return Task.CompletedTask;
        }
    }
}
