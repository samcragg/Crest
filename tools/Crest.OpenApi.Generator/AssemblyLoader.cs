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
    using System.Runtime.Loader;
    using Microsoft.Extensions.DependencyModel;
    using Microsoft.Extensions.DependencyModel.Resolution;

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
        private readonly ICompilationAssemblyResolver resolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyLoader"/> class.
        /// </summary>
        /// <param name="path">The full path of the assembly to load.</param>
        public AssemblyLoader(string path)
        {
            this.assemblyContext = AssemblyLoadContext.Default;
            this.Assembly = this.assemblyContext.LoadFromAssemblyPath(path);
            this.dependencyContext = DependencyContext.Load(this.Assembly);

            string assemblyDirectory = Path.GetDirectoryName(path);
            this.resolver = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
            {
                new AppBaseCompilationAssemblyResolver(assemblyDirectory),
                new ReferenceAssemblyPathResolver(),
                new PackageCompilationAssemblyResolver()
            });

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

        private static CompilationLibrary ConvertLibrary(RuntimeLibrary library)
        {
            return new CompilationLibrary(
                library.Type,
                library.Name,
                library.Version,
                library.Hash,
                library.RuntimeAssemblyGroups.SelectMany(g => g.AssetPaths),
                library.Dependencies,
                library.Serviceable);
        }

        private Assembly OnAssemblyContextResolving(AssemblyLoadContext context, AssemblyName name)
        {
            RuntimeLibrary library =
                this.dependencyContext.RuntimeLibraries
                    .FirstOrDefault(lib => string.Equals(lib.Name, name.Name, StringComparison.OrdinalIgnoreCase));

            if (library != null)
            {
                // Although TryResolveAssemblyPaths returns false on error, it
                // also returns true if no assemblies were resolved, hence the
                // check for the count instead
                var assemblies = new List<string>();
                this.resolver.TryResolveAssemblyPaths(ConvertLibrary(library), assemblies);
                if (assemblies.Count > 0)
                {
                    return this.assemblyContext.LoadFromAssemblyPath(assemblies[0]);
                }
            }

            Trace.Warning("Unable to resolve '{0}'", name.FullName);
            return null;
        }
    }
}
