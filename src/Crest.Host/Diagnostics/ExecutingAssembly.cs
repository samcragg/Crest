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
    using Crest.Core.Logging;
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
                "MICROSOFT",
                "NEWTONSOFT",
                "SYSTEM",
            }, StringComparer.Ordinal);

        private static readonly ILog Logger = Log.For<ExecutingAssembly>();
        private static DependencyContext overrideContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutingAssembly"/> class.
        /// </summary>
        public ExecutingAssembly()
        {
        }

        /// <summary>
        /// Gets or sets the loaded DependencyContext.
        /// </summary>
        internal static DependencyContext DependencyContext
        {
            get => overrideContext ?? DependencyContext.Default;
            set => overrideContext = value;
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
            return DependencyContext
                .CompileLibraries
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => new AssemblyInfo(x));
        }

        /// <summary>
        /// Loads the assemblies that were compiled against the current assembly.
        /// </summary>
        /// <returns>The loaded assemblies.</returns>
        public virtual IEnumerable<Assembly> LoadCompileLibraries()
        {
            foreach (CompilationLibrary library in DependencyContext.CompileLibraries)
            {
                string prefix = GetAssemblyPrefix(library.Name);
                if (ExcludedAssemblies.Contains(prefix))
                {
                    Logger.Debug("Skipping scanning of {library}", library.Name);
                }
                else
                {
                    Logger.Info("Discovered library {library} at {path}", library.Name, library.Path);
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
                return name.ToUpperInvariant();
            }
            else
            {
                return name.Substring(0, dot).ToUpperInvariant();
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
                Logger.Warn("Unable to load library {name}", name);
                return null;
            }
        }
    }
}
