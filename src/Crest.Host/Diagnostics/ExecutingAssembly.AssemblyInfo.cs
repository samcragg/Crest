// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using Microsoft.Extensions.DependencyModel;

    /// <content>
    /// Contains the nested helper <see cref="AssemblyInfo"/> struct.
    /// </content>
    internal partial class ExecutingAssembly
    {
        /// <summary>
        /// Contains basic information about an assembly.
        /// </summary>
        internal struct AssemblyInfo
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="AssemblyInfo"/> struct.
            /// </summary>
            /// <param name="library">Contains the assembly information.</param>
            internal AssemblyInfo(CompilationLibrary library)
                : this(library.Name, library.Version)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="AssemblyInfo"/> struct.
            /// </summary>
            /// <param name="name">The name of the assembly.</param>
            /// <param name="version">The version of the assembly.</param>
            internal AssemblyInfo(string name, string version)
            {
                this.Name = name;
                this.Version = version;
            }

            /// <summary>
            /// Gets the name of the assembly.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Gets the version of the assembly.
            /// </summary>
            public string Version { get; }
        }
    }
}
