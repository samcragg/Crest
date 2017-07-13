// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Wraps access to IO resources.
    /// </summary>
    internal class IOAdapter
    {
        private readonly Assembly assembly = typeof(IOAdapter).GetTypeInfo().Assembly;

        /// <summary>
        /// Returns an enumerable collection of file names that match a search
        /// pattern in a specified path and subdirectories.
        /// </summary>
        /// <param name="path">
        /// The relative or absolute path to the directory to search.
        /// </param>
        /// <param name="searchPattern">
        /// The search string to match against the names of files in path.
        /// </param>
        /// <returns>
        /// An enumerable collection of the full names, including paths.
        /// </returns>
        public virtual IEnumerable<string> EnumerateFiles(string path, string searchPattern)
        {
            return Directory.EnumerateFiles(path, searchPattern, SearchOption.AllDirectories);
        }

        /// <summary>
        /// Gets the pathname of the base directory that the assembly resolver
        /// uses to probe for assemblies.
        /// </summary>
        /// <returns>
        /// The pathname of the base directory that the assembly resolver uses
        /// to probe for assemblies.
        /// </returns>
        public virtual string GetBaseDirectory()
        {
            return AppContext.BaseDirectory;
        }

        /// <summary>
        /// Opens an existing file for reading.
        /// </summary>
        /// <param name="path">The file to be opened for reading.</param>
        /// <returns>
        /// A read-only <see cref="FileStream"/> on the specified path.
        /// </returns>
        public virtual Stream OpenRead(string path)
        {
            const int DefaultBufferSize = 4 * 1024;
            return new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                DefaultBufferSize,
                useAsync: true);
        }

        /// <summary>
        /// Loads the specified manifest resource from the current assembly.
        /// </summary>
        /// <param name="name">
        /// The case-sensitive name of the manifest resource being requested.
        /// </param>
        /// <returns>
        /// The manifest resource or <c>null</c> if no resources were specified
        /// during compilation.
        /// </returns>
        public virtual Stream OpenResource(string name)
        {
            return this.assembly.GetManifestResourceStream(name);
        }
    }
}
