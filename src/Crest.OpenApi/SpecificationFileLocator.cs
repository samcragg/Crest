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
    internal class SpecificationFileLocator
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
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecificationFileLocator"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is only used to allow the type to be mocked in unit tests.
        /// </remarks>
        protected SpecificationFileLocator()
        {
        }

        /// <summary>
        /// Gets the relative paths to documentation specification files.
        /// </summary>
        public virtual IReadOnlyList<string> RelativePaths { get; }

        private static string[] FindFiles(IOAdapter io)
        {
            string currentDirectory = io.GetBaseDirectory();
            string searchDirectory = Path.Combine(currentDirectory, DocsDirectory);
            var baseUri = new Uri(searchDirectory + Path.DirectorySeparatorChar);

            string MakeRelative(string fullPath)
            {
                var url = new Uri(fullPath);
                Uri relative = baseUri.MakeRelativeUri(url);
                return Uri.UnescapeDataString(relative.ToString());
            }

            // When enumerating the files, we should prefer json.gz to json so
            // search for both file names (the Concat) and then group them by
            // their directory name. For each grouping, the json.gz is longer
            // so order it by the length (descending for longest first) and
            // select the first one in the group (which will be json.gz if it's
            // there, otherwise, the group will have the single json file)
            return io.EnumerateFiles(searchDirectory, "openapi.json")
                     .Concat(io.EnumerateFiles(searchDirectory, "openapi.json.gz"))
                     .GroupBy(path => Path.GetDirectoryName(path))
                     .Select(group => group.OrderByDescending(p => p.Length).First())
                     .Select(MakeRelative)
                     .ToArray();
        }
    }
}
