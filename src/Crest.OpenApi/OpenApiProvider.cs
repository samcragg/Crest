// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Crest.Abstractions;

    /// <summary>
    /// Sends the Open API and Swagger UI files.
    /// </summary>
    internal sealed class OpenApiProvider : IDirectRouteProvider
    {
        private const string Css = "text/css";
        private const string Html = "text/html";
        private const string Javascript = "application/javascript";
        private const string Json = "application/json";
        private readonly IOAdapter adapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenApiProvider"/> class.
        /// </summary>
        /// <param name="adapter">Allows access to the IO sub-systems.</param>
        public OpenApiProvider(IOAdapter adapter)
        {
            this.adapter = adapter;
        }

        /// <inheritdoc />
        public IEnumerable<DirectRouteMetadata> GetDirectRoutes()
        {
            yield return this.CreateRouteFromFile("/doc/openapi.json", "openapi.json", Json);
            yield return this.CreateRedirect("/doc", "/doc/index.html");
            yield return this.CreateRouteFromResource("/doc/index.html", "index.html", Html);
            yield return this.CreateRouteFromResource("/doc/swagger-ui.css", "swagger-ui.css.gz", Css);
            yield return this.CreateRouteFromResource("/doc/swagger-ui-bundle.js", "swagger-ui-bundle.js.gz", Javascript);
            yield return this.CreateRouteFromResource("/doc/swagger-ui-standalone-preset.js", "swagger-ui-standalone-preset.js.gz", Javascript);
        }

        private DirectRouteMetadata CreateRedirect(string from, string to)
        {
            var redirect = Task.FromResult<IResponseData>(new RedirectResponse(to));
            return new DirectRouteMetadata
            {
                Method = (r, c) => redirect,
                RouteUrl = from,
                Verb = "GET"
            };
        }

        private DirectRouteMetadata CreateRouteFromFile(string route, string path, string contentType)
        {
            var response = Task.FromResult<IResponseData>(
                new StreamResponse(() => this.adapter.OpenRead(path))
                {
                    ContentType = contentType
                });

            return new DirectRouteMetadata
            {
                Method = (r, c) => response,
                RouteUrl = route,
                Verb = "GET"
            };
        }

        private DirectRouteMetadata CreateRouteFromResource(string route, string resourceName, string contentType)
        {
            Task<IResponseData> CreateResponse(IRequestData request, IContentConverter converter)
            {
                string resource = "Crest.OpenApi.SwaggerUI." + resourceName;
                Func<Stream> source = () => this.adapter.OpenResource(resource);

                // If the resource is compressed then let the StreamResponse
                // know about what compression the client accepts - otherwise
                // we just return the resource without adding the compression
                // header.
                StreamResponse response;
                if (resourceName.EndsWith(".gz", StringComparison.Ordinal))
                {
                    response = new StreamResponse(source, request.Headers["Accept-Encoding"]);
                }
                else
                {
                    response = new StreamResponse(source);
                }

                response.ContentType = contentType;
                return Task.FromResult<IResponseData>(response);
            }

            return new DirectRouteMetadata
            {
                Method = CreateResponse,
                RouteUrl = route,
                Verb = "GET"
            };
        }
    }
}
