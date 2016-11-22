// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Crest.Host;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Allows the conversion from the ASP .NET context to a request data.
    /// </summary>
    internal sealed class HttpContextRequestData : IRequestData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContextRequestData"/> class.
        /// </summary>
        /// <param name="handler">The route handler.</param>
        /// <param name="parameters">The parameters for the method.</param>
        /// <param name="context">The current request context.</param>
        public HttpContextRequestData(MethodInfo handler, IReadOnlyDictionary<string, object> parameters, HttpContext context)
        {
            this.Context = context;
            this.Handler = handler;
            this.Headers = new HeadersAdapter(context.Request.Headers);
            this.Parameters = parameters;
            this.Url = ConvertToUri(context.Request);
        }

        /// <inheritdoc />
        public MethodInfo Handler { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Parameters { get; }

        /// <inheritdoc />
        public Uri Url { get; }

        /// <summary>
        /// Gets the current request context.
        /// </summary>
        internal HttpContext Context { get; }

        private static Uri ConvertToUri(HttpRequest request)
        {
            var builder = new UriBuilder
            {
                Host = request.Host.Host,
                Path = request.Path,
                Port = request.Host.Port.GetValueOrDefault(-1),
                Query = request.QueryString.Value,
                Scheme = request.Scheme
            };

            return builder.Uri;
        }
    }
}
