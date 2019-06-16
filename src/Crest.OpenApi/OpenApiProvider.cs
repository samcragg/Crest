// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Crest.Abstractions;

    /// <summary>
    /// Sends the Open API and Swagger UI files.
    /// </summary>
    internal sealed class OpenApiProvider : IDirectRouteProvider
    {
        /// <summary>
        /// Represents the base route added to all the documentation routes.
        /// </summary>
        internal const string DocumentationBaseRoute = "/docs";

        private const string Css = "text/css";
        private const string Html = "text/html";
        private const string Javascript = "application/javascript";
        private readonly IndexHtmlGenerator generator;
        private readonly IOAdapter io;
        private readonly SpecificationFileLocator specFiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenApiProvider"/> class.
        /// </summary>
        /// <param name="io">Allows access to the IO sub-systems.</param>
        /// <param name="specFiles">Locates the specification files.</param>
        /// <param name="generator">Generates the index page.</param>
        public OpenApiProvider(IOAdapter io, SpecificationFileLocator specFiles, IndexHtmlGenerator generator)
        {
            this.generator = generator;
            this.io = io;
            this.specFiles = specFiles;
        }

        /// <inheritdoc />
        public IEnumerable<DirectRouteMetadata> GetDirectRoutes()
        {
            yield return this.CreateRedirect(
                DocumentationBaseRoute,
                DocumentationBaseRoute + "/index.html");

            yield return CreateMetadata(
                DocumentationBaseRoute + "/index.html",
                Html,
                "index.html",
                this.generator.GetPage);

            yield return this.CreateRouteFromResource(
                DocumentationBaseRoute + "/swagger-ui.css",
                "swagger-ui.css.gz",
                Css);

            yield return this.CreateRouteFromResource(
                DocumentationBaseRoute + "/swagger-ui-bundle.js",
                "swagger-ui-bundle.js.gz",
                Javascript);

            yield return this.CreateRouteFromResource(
                DocumentationBaseRoute + "/swagger-ui-standalone-preset.js",
                "swagger-ui-standalone-preset.js.gz",
                Javascript);

            foreach (DirectRouteMetadata metadata in this.GetSpecificationFiles())
            {
                yield return metadata;
            }
        }

        private static DirectRouteMetadata CreateMetadata(string route, string contentType, string name, Func<Stream> source)
        {
            Task<IResponseData> CreateResponse(IRequestData request, IContentConverter converter)
            {
                // If the resource is compressed then let the StreamResponse
                // know about what compression the client accepts - otherwise
                // we just return the resource without adding the compression
                // header.
                StreamResponse response;
                if (name.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
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
                Path = route,
                Verb = "GET",
            };
        }

        private DirectRouteMetadata CreateRedirect(string from, string to)
        {
            var redirect = Task.FromResult<IResponseData>(new RedirectResponse(to));
            return new DirectRouteMetadata
            {
                Method = (r, c) => redirect,
                Path = from,
                Verb = "GET",
            };
        }

        private DirectRouteMetadata CreateRouteFromFile(string route, string path, string contentType)
        {
            string fullPath = Path.Combine(
                this.io.GetBaseDirectory(),
                SpecificationFileLocator.DocsDirectory,
                path);

            return CreateMetadata(
                route,
                contentType,
                path,
                () => this.io.OpenRead(fullPath));
        }

        private DirectRouteMetadata CreateRouteFromResource(string route, string resourceName, string contentType)
        {
            string resource = "Crest.OpenApi.SwaggerUI." + resourceName;
            return CreateMetadata(
                route,
                contentType,
                resourceName,
                () => this.io.OpenResource(resource));
        }

        private IEnumerable<DirectRouteMetadata> GetSpecificationFiles()
        {
            DirectRouteMetadata ExposeFile(string path)
            {
                // Normalize the file ending so we serve openapi.json.gz as openapi.json
                string route = path;
                if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                {
                    route = route.Substring(0, route.Length - 3);
                }

                return this.CreateRouteFromFile(DocumentationBaseRoute + "/" + route, path, Javascript);
            }

            return this.specFiles.RelativePaths.Select(ExposeFile);
        }
    }
}
