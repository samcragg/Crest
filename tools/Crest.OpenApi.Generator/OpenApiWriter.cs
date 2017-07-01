// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi.Generator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Writes an OpenAPI document for a specific version.
    /// </summary>
    internal sealed class OpenApiWriter : JsonWriter
    {
        private readonly DefinitionWriter definitions;
        private readonly InfoObjectWriter info;
        private readonly OperationObjectWriter operations;
        private readonly TagWriter tags;
        private readonly int version;
        private readonly XmlDocParser xmlDoc;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenApiWriter"/> class.
        /// </summary>
        /// <param name="xmlDoc">Contains the parsed XML documentation.</param>
        /// <param name="writer">Where to write the output to.</param>
        /// <param name="version">The version of the routes to write.</param>
        public OpenApiWriter(XmlDocParser xmlDoc, TextWriter writer, int version)
            : base(writer)
        {
            this.version = version;
            this.xmlDoc = xmlDoc;
            this.definitions = new DefinitionWriter(this.xmlDoc, writer);
            this.info = new InfoObjectWriter(writer);
            this.tags = new TagWriter(this.xmlDoc, writer);
            this.operations = new OperationObjectWriter(this.xmlDoc, this.definitions, this.tags, writer);
        }

        /// <summary>
        /// Writes additional information at the end of the document.
        /// </summary>
        public void WriteFooter()
        {
            this.WriteRaw(",\"definitions\":");
            this.definitions.WriteDefinitions();

            this.WriteRaw(",\"tags\":");
            this.tags.WriteTags();

            this.Write('}');
        }

        /// <summary>
        /// Writes the OpenAPI document header information.
        /// </summary>
        /// <param name="assembly">The assembly information.</param>
        public void WriteHeader(Assembly assembly)
        {
            this.WriteRaw("{\"swagger\":\"2.0\",\"info\":{");
            this.info.WriteInformation(this.version, assembly);
            this.WriteRaw("},\"basePath\":");
            this.WriteString("/v" + this.version);
        }

        /// <summary>
        /// Writes the operations for the specified type.
        /// </summary>
        /// <param name="routes">The route information to write.</param>
        public void WriteOperations(IReadOnlyCollection<RouteInformation> routes)
        {
            this.WriteRaw(",\"paths\":{");

            // Sort and group routes by to allow DELETE and POST of the same URL
            IEnumerable<IGrouping<string, RouteInformation>> operations =
                routes.Where(this.RouteIsAvailable)
                      .GroupBy(r => NormalizeRoute(r.Route))
                      .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

            this.WriteList(
                operations,
                operation => this.WriteOperation(operation.Key, operation));

            this.Write('}');
        }

        private static string NormalizeRoute(string routeUrl)
        {
            // The spec state the path MUST begin with a slash
            if ((routeUrl ?? string.Empty).StartsWith("/"))
            {
                return routeUrl;
            }
            else
            {
                return "/" + routeUrl;
            }
        }

        private bool RouteIsAvailable(RouteInformation route)
        {
            return (route.MinVersion <= this.version) && (this.version <= route.MaxVersion);
        }

        private void WriteOperation(string routeUrl, IEnumerable<RouteInformation> routes)
        {
            this.WriteString(routeUrl);
            this.WriteRaw(":{");

            this.WriteList(routes, route =>
            {
                this.WriteString(route.Verb);
                this.Write(':');
                this.operations.WriteOperation(route.Route, route.Method);
            });

            this.Write('}');
        }
    }
}
