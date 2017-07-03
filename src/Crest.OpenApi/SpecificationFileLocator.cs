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

    /// <summary>
    /// Allows the locating of the OpenAPI JSON specification files for the
    /// various versions of the service.
    /// </summary>
    internal sealed class SpecificationFileLocator
    {
        /// <summary>
        /// Represents the base directory, relative to the current directory,
        /// where documation files will be search in.
        /// </summary>
        internal const string DocsDirectory = "docs";

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecificationFileLocator"/> class.
        /// </summary>
        /// <param name="io">Allows access to the IO sub-systems.</param>
        public SpecificationFileLocator(IOAdapter io)
        {
            this.RelativePaths = FindFiles(io);
            this.Latest =
                this.RelativePaths
                    .OrderByDescending(x => x, new StringVersionComparer())
                    .FirstOrDefault();
        }

        /// <summary>
        /// Gets the path of the highest version.
        /// </summary>
        public string Latest { get; }

        /// <summary>
        /// Gets the relative paths to documentation specification files.
        /// </summary>
        public IReadOnlyList<string> RelativePaths { get; }

        private static string[] FindFiles(IOAdapter io)
        {
            string currentDirectory = io.GetCurrentDirectory();
            string searchDirectory = Path.Combine(currentDirectory, DocsDirectory);
            var baseUri = new Uri(currentDirectory + "\\");

            string MakeRelative(string fullPath)
            {
                var url = new Uri(fullPath);
                Uri relative = baseUri.MakeRelativeUri(url);
                return Uri.UnescapeDataString(relative.ToString());
            }

            return io.EnumerateFiles(searchDirectory, "openapi.json")
                     .Select(MakeRelative)
                     .ToArray();
        }
    }
}
