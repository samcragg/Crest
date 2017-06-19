// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Wraps access to IO resources.
    /// </summary>
    internal class IOAdapter
    {
        private readonly Assembly assembly = typeof(IOAdapter).GetTypeInfo().Assembly;

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
    }
}
