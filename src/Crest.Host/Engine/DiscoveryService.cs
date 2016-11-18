// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using Crest.Core;
    using Microsoft.Extensions.DependencyModel;

    /// <summary>
    /// Uses the DependencyModel package to find dependencies at runtime.
    /// </summary>
    internal sealed class DiscoveryService : IDiscoveryService
    {
        private static readonly ISet<string> ExcludedNamespaces = new HashSet<string>(
            new[]
            {
                "DryIoc",
                "Microsoft",
                "Newtonsoft",
                "System"
            }, StringComparer.Ordinal);

        private readonly DependencyContext context;
        private readonly Lazy<IReadOnlyList<Type>> loadedTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryService"/> class.
        /// </summary>
        /// <param name="entryAssembly">
        /// The assembly to load the dependency context of.
        /// </param>
        public DiscoveryService(Assembly entryAssembly = null)
        {
            this.context = DependencyContext.Load(entryAssembly ?? Assembly.GetEntryAssembly());
            this.loadedTypes = new Lazy<IReadOnlyList<Type>>(this.LoadTypes, LazyThreadSafetyMode.None);
        }

        /// <summary>
        /// Gets or sets the function to load an assembly.
        /// </summary>
        /// <remarks>Exposed for unit testing.</remarks>
        internal Func<AssemblyName, Assembly> AssemblyLoad { get; set; } = Assembly.Load;

        /// <inheritdoc />
        public IEnumerable<ITypeFactory> GetCustomFactories()
        {
            return from type in this.loadedTypes.Value
                   where typeof(ITypeFactory).IsAssignableFrom(type)
                   let typeInfo = type.GetTypeInfo()
                   where !typeInfo.IsAbstract && !typeInfo.IsInterface
                   select (ITypeFactory)Activator.CreateInstance(type);
        }

        /// <inheritdoc />
        public IEnumerable<Type> GetDiscoveredTypes()
        {
            return this.loadedTypes.Value;
        }

        /// <inheritdoc />
        public IEnumerable<RouteMetadata> GetRoutes(Type type)
        {
            foreach (MethodInfo method in type.GetTypeInfo().DeclaredMethods)
            {
                string verb = null;
                foreach (RouteAttribute route in method.GetCustomAttributes<RouteAttribute>())
                {
                    if (verb == null)
                    {
                        verb = route.Verb;
                    }
                    else if (!verb.Equals(route.Verb, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Multiple HTTP verbs are not allowed.");
                    }

                    yield return new RouteMetadata
                    {
                        Method = method,
                        RouteUrl = route.Route,
                        Verb = verb
                    };
                }
            }
        }

        /// <inheritdoc />
        public bool IsSingleInstance(Type type)
        {
            return false;
        }

        private static string GetRootNamespace(string ns)
        {
            if (string.IsNullOrEmpty(ns))
            {
                return string.Empty;
            }
            else
            {
                int separator = ns.IndexOf('.');
                if (separator < 0)
                {
                    return ns;
                }
                else
                {
                    return ns.Substring(0, separator);
                }
            }
        }

        private static bool IncludeType(TypeInfo typeInfo)
        {
            // Quick check against the namespace of the type
            string root = GetRootNamespace(typeInfo.Namespace);
            if (!ExcludedNamespaces.Contains(root))
            {
                // More expensive check to exclude generated types
                return !typeInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
            }
            else
            {
                return false;
            }
        }

        private IEnumerable<TypeInfo> GetAssemblyTypes(string name)
        {
            try
            {
                Assembly assembly = this.AssemblyLoad(new AssemblyName(name));
                return assembly.DefinedTypes;
            }
            catch
            {
                return Enumerable.Empty<TypeInfo>();
            }
        }

        private IReadOnlyList<Type> LoadTypes()
        {
            IEnumerable<Type> types =
                from library in this.context.CompileLibraries
                from typeInfo in this.GetAssemblyTypes(library.Name)
                where IncludeType(typeInfo)
                select typeInfo.AsType();

            return types.ToList();
        }
    }
}
