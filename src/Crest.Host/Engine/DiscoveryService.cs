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
    using Crest.Abstractions;
    using Crest.Core;
    using Crest.Host.Diagnostics;

    /// <summary>
    /// Uses the DependencyModel package to find dependencies at runtime.
    /// </summary>
    internal sealed class DiscoveryService : IDiscoveryService
    {
        private static readonly ISet<string> ExcludedNamespaces = new HashSet<string>(
            new[]
            {
                "DryIoc",
                "ImTools",
                "Microsoft",
                "MS",
                "Newtonsoft",
                "System"
            }, StringComparer.Ordinal);

        private readonly ExecutingAssembly assemblyInfo;
        private readonly Lazy<IReadOnlyList<Type>> loadedTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryService"/> class.
        /// </summary>
        public DiscoveryService()
            : this(new ExecutingAssembly())
        {
            // Note the poor mans dependency injection. Since this class is used
            // to find the types to register in the IoC container, we can't
            // have parameters injected into us, hence the above which allows us
            // to unit test the class but also be created at runtime.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryService"/> class.
        /// </summary>
        /// <param name="assemblyInfo">
        /// Contains information about the current assembly.
        /// </param>
        internal DiscoveryService(ExecutingAssembly assemblyInfo)
        {
            this.assemblyInfo = assemblyInfo;
            this.loadedTypes = new Lazy<IReadOnlyList<Type>>(this.LoadTypes, LazyThreadSafetyMode.None);
        }

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
                VersionAttribute version = null; // Lazy load this in case there are no routes on the method
                foreach (RouteAttribute route in method.GetCustomAttributes<RouteAttribute>())
                {
                    if (verb == null)
                    {
                        verb = route.Verb;
                        version = GetVersionInformation(method);
                    }
                    else if (!verb.Equals(route.Verb, StringComparison.OrdinalIgnoreCase))
                    {
                        throw CreateException(method, "Multiple HTTP verbs are not allowed.");
                    }

                    yield return new RouteMetadata
                    {
                        MaximumVersion = version.To,
                        Method = method,
                        MinimumVersion = version.From,
                        RouteUrl = route.Route,
                        Verb = verb
                    };
                }
            }
        }

        /// <inheritdoc />
        public bool IsSingleInstance(Type type)
        {
            // Because this type is a cache of generated classes, it needs to
            // be single instance.
            return type == typeof(Serialization.SerializerGenerator<>);
        }

        private static InvalidOperationException CreateException(MethodInfo method, string message)
        {
            return new InvalidOperationException(
                "Error getting routes for method " + method.Name + ": " + message);
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

        private static VersionAttribute GetVersionInformation(MethodInfo method)
        {
            VersionAttribute attribute = method.GetCustomAttribute<VersionAttribute>();
            if (attribute == null)
            {
                throw CreateException(method, "No Version attribute found on method.");
            }

            if (attribute.From > attribute.To)
            {
                throw CreateException(method, "From must be less than or equal to To");
            }

            return attribute;
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

        private IReadOnlyList<Type> LoadTypes()
        {
            IEnumerable<Type> types =
                from assembly in this.assemblyInfo.LoadCompileLibraries()
                from typeInfo in assembly.DefinedTypes
                where IncludeType(typeInfo)
                select typeInfo.AsType();

            return types.ToList();
        }
    }
}
