// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;
    using Microsoft.Extensions.DependencyModel;

    /// <summary>
    /// Helper class to load an assembly and it's dependencies.
    /// </summary>
    /// <remarks>
    /// Assembly loading isn't as easy in .NET Core...
    /// </remarks>
    internal sealed class AssemblyLoader : IDisposable
    {
        private readonly AssemblyLoadContext assemblyContext;
        private readonly DependencyContext dependencyContext;
        private readonly string assemblyDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyLoader"/> class.
        /// </summary>
        /// <param name="path">The full path of the assembly to load.</param>
        public AssemblyLoader(string path)
        {
            this.assemblyContext = AssemblyLoadContext.Default;
            this.Assembly = this.assemblyContext.LoadFromAssemblyPath(path);
            this.assemblyDirectory = Path.GetDirectoryName(path);
            this.dependencyContext = DependencyContext.Load(this.Assembly);

            // Do this after we have assigned all the variables
            this.assemblyContext.Resolving += this.OnAssemblyContextResolving;
        }

        /// <summary>
        /// Gets the loaded assembly.
        /// </summary>
        public Assembly Assembly { get; }

        /// <summary>
        /// Releases resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            this.assemblyContext.Resolving -= this.OnAssemblyContextResolving;
        }

        private Assembly OnAssemblyContextResolving(AssemblyLoadContext context, AssemblyName name)
        {
            CompilationLibrary library =
                this.dependencyContext.CompileLibraries
                    .FirstOrDefault(rl => string.Equals(rl.Name, name.Name, StringComparison.Ordinal));

            if (library != null)
            {
                foreach (string assembly in library.Assemblies)
                {
                    string path = Path.Combine(this.assemblyDirectory, assembly);
                    if (File.Exists(path))
                    {
                        return this.assemblyContext.LoadFromAssemblyPath(path);
                    }
                }
            }

            return null;
        }
    }
}
