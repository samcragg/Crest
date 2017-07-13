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
    using System.Text;

    /// <summary>
    /// Generates the HTML for the documentation home page.
    /// </summary>
    internal class IndexHtmlGenerator
    {
        private const string UrlsPlaceholder = "#URLS#";
        private readonly byte[] page;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexHtmlGenerator"/> class.
        /// </summary>
        /// <param name="io">Allows access to the IO sub-systems.</param>
        /// <param name="specFiles">Locates the specification files.</param>
        public IndexHtmlGenerator(IOAdapter io, SpecificationFileLocator specFiles)
        {
            string html = LoadIndexHtmlResource(io);
            string urls = CreateUrlsJson(specFiles);
            this.page = Encoding.UTF8.GetBytes(html.Replace(UrlsPlaceholder, urls));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexHtmlGenerator"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is only used to allow the type to be mocked in unit tests.
        /// </remarks>
        protected IndexHtmlGenerator()
        {
        }

        /// <summary>
        /// Gets a stream that contains the generate page content.
        /// </summary>
        /// <returns>A stream that contains the generate page.</returns>
        public virtual Stream GetPage()
        {
            return new MemoryStream(this.page, writable: false);
        }

        private static string CreateUrlsJson(SpecificationFileLocator specFiles)
        {
            var buffer = new StringBuilder();
            buffer.Append("\"urls\":[");

            IEnumerable<string> paths =
                specFiles.RelativePaths
                         .OrderByDescending(x => x, new StringVersionComparer());

            bool first = true;
            foreach (string path in paths)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    buffer.Append(',');
                }

                AppendUrl(buffer, path);
            }

            buffer.AppendLine("]");
            return buffer.ToString();
        }

        private static void AppendUrl(StringBuilder buffer, string path)
        {
            buffer.Append("{\"url\":\"" + OpenApiProvider.DocumentationBaseRoute + "/");
            if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            {
                buffer.Append(path, 0, path.Length - 3);
            }
            else
            {
                buffer.Append(path);
            }

            buffer.Append("\",\"name\":\"")
                  .Append(GetVersion(path))
                  .Append("\"}");
        }

        private static string GetVersion(string path)
        {
            int end = path.IndexOf('/');
            return path.Substring(0, end).ToUpperInvariant();
        }

        private static string LoadIndexHtmlResource(IOAdapter io)
        {
            const string IndexHtml = "Crest.OpenApi.SwaggerUI.index.html";
            using (var reader = new StreamReader(io.OpenResource(IndexHtml), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
