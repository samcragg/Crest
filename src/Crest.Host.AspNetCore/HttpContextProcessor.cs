// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.AspNetCore
{
    using System.Net;
    using System.Threading.Tasks;
    using Crest.Host;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Allows the orchestration of the handling of a request.
    /// </summary>
    internal sealed class HttpContextProcessor : RequestProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContextProcessor"/> class.
        /// </summary>
        /// <param name="bootstrapper">Contains application settings.</param>
        public HttpContextProcessor(Bootstrapper bootstrapper)
            : base(bootstrapper)
        {
        }

        /// <summary>
        /// Handles the request, adapting the ASP.NET Core objects to enable
        /// processing by the Crest framework.
        /// </summary>
        /// <param name="context">The HTTP request.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task HandleRequest(HttpContext context)
        {
            MatchResult result = this.Match(
                context.Request.Method,
                context.Request.Path.Value,
                new QueryLookup(context.Request.QueryString.Value));

            if (result.Success)
            {
                var data = new HttpContextRequestData(result.Method, result.Parameters, context);
                return this.HandleRequest(data);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return Task.CompletedTask;
            }
        }

        /// <inheritdoc />
        protected override Task WriteResponse(IRequestData request, IResponseData response)
        {
            HttpContext context = ((HttpContextRequestData)request).Context;
            context.Response.ContentType = response.ContentType;
            context.Response.StatusCode = response.StatusCode;
            return response.WriteBody(context.Response.Body);
        }
    }
}
