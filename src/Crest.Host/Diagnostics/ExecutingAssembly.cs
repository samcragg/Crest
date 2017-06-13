// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Microsoft.Extensions.DependencyModel;

    /// <summary>
    /// Provides functionality for getting information about the executing
    /// assembly.
    /// </summary>
    internal partial class ExecutingAssembly
    {
        private static readonly ISet<string> ExcludedAssemblies = new HashSet<string>(
            new[]
            {
                "microsoft",
                "newtonsoft",
                "system"
            }, StringComparer.Ordinal);

        private static Lazy<DependencyContext> dependencyContext;

        static ExecutingAssembly()
        {
            dependencyContext = new Lazy<DependencyContext>(
                () => DependencyContext.Default,
                LazyThreadSafetyMode.None);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutingAssembly"/> class.
        /// </summary>
        public ExecutingAssembly()
            : this(Assembly.GetEntryAssembly())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutingAssembly"/> class.
        /// </summary>
        /// <param name="entryAssembly">
        /// The assembly to load the dependency context of.
        /// </param>
        public ExecutingAssembly(Assembly entryAssembly)
        {
            dependencyContext = new Lazy<DependencyContext>(
                () => DependencyContext.Load(entryAssembly),
                LazyThreadSafetyMode.None);
        }

        /// <summary>
        /// Gets or sets the function to load an assembly.
        /// </summary>
        /// <remarks>Exposed for unit testing.</remarks>
        internal Func<AssemblyName, Assembly> AssemblyLoad { get; set; } = Assembly.Load;

        /// <summary>
        /// Gets the libraries that were compiled against the current assembly.
        /// </summary>
        /// <returns>A sequence of assembly informations.</returns>
        public virtual IEnumerable<AssemblyInfo> GetCompileLibraries()
        {
            return dependencyContext.Value
                .CompileLibraries
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => new AssemblyInfo(x));
        }

        /// <summary>
        /// Loads the assemblies that were compiled agains the current assembly.
        /// </summary>
        /// <returns>The loaded assemblies.</returns>
        public virtual IEnumerable<Assembly> LoadCompileLibraries()
        {
            foreach (CompilationLibrary library in dependencyContext.Value.CompileLibraries)
            {
                string prefix = GetAssemblyPrefix(library.Name);
                if (!ExcludedAssemblies.Contains(prefix))
                {
                    Assembly assembly = this.LoadAssembly(library.Name);
                    if (assembly != null)
                    {
                        yield return assembly;
                    }
                }
            }
        }

        private static string GetAssemblyPrefix(string name)
        {
            int dot = name.IndexOf('.');
            if (dot < 0)
            {
                return name.ToLowerInvariant();
            }
            else
            {
                return name.Substring(0, dot).ToLowerInvariant();
            }
        }

        private Assembly LoadAssembly(string name)
        {
            try
            {
                return this.AssemblyLoad(new AssemblyName(name));
            }
            catch
            {
                return null;
            }
        }
    }
}
