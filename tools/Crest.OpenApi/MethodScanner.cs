﻿// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Scans an assembly for route methods and their version.
    /// </summary>
    internal sealed class MethodScanner
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodScanner"/> class.
        /// </summary>
        /// <param name="assembly">The assembly to scan for routes.</param>
        public MethodScanner(Assembly assembly)
        {
            int maximum = 0;
            int minimum = 0;
            var routes = new List<RouteInformation>();
            foreach (RouteInformation route in this.ScanRoutes(assembly))
            {
                routes.Add(route);

                if ((minimum == 0) || (route.MinVersion < minimum))
                {
                    minimum = route.MinVersion;
                }

                if ((route.MaxVersion != int.MaxValue) && (route.MaxVersion > maximum))
                {
                    maximum = route.MaxVersion;
                }
            }

            this.MaximumVersion = (maximum < minimum) ? minimum : maximum;
            this.MinimumVersion = minimum;
            this.Routes = routes;
        }

        /// <summary>
        /// Gets the maximum version of any route that has one defined.
        /// </summary>
        public int MaximumVersion { get; }

        /// <summary>
        /// Gets the minimum version of all the scanned routes.
        /// </summary>
        public int MinimumVersion { get; }

        /// <summary>
        /// Gets the routes found from scanning the assembly.
        /// </summary>
        public IReadOnlyCollection<RouteInformation> Routes { get; }

        private static bool TryGetRoute(CustomAttributeData attribute, ref string verb, out string route)
        {
            switch (attribute.AttributeType.Name)
            {
                case "DeleteAttribute":
                    verb = "delete";
                    break;

                case "GetAttribute":
                    verb = "get";
                    break;

                case "PostAttribute":
                    verb = "post";
                    break;

                case "PutAttribute":
                    verb = "put";
                    break;

                default:
                    route = null;
                    return false;
            }

            route = (string)attribute.ConstructorArguments[0].Value;
            return true;
        }

        private static void TryGetVersion(CustomAttributeData attribute, ref int minimum, ref int maximum)
        {
            if (attribute.AttributeType.Name == "VersionAttribute")
            {
                minimum = (int)attribute.ConstructorArguments[0].Value;
                if (attribute.ConstructorArguments.Count == 2)
                {
                    maximum = (int)attribute.ConstructorArguments[1].Value;
                }
                else
                {
                    maximum = int.MaxValue;
                }
            }
        }

        private IEnumerable<RouteInformation> ScanRoutes(Assembly assembly)
        {
            foreach (Type type in assembly.ExportedTypes)
            {
                foreach (MethodInfo method in type.GetMethods())
                {
                    int minimum = 1; // Default to version one
                    int maximum = int.MaxValue;
                    string verb = null;
                    var routes = new List<string>();

                    foreach (CustomAttributeData attribute in method.CustomAttributes)
                    {
                        string route;
                        if (TryGetRoute(attribute, ref verb, out route))
                        {
                            routes.Add(route);
                        }
                        else
                        {
                            TryGetVersion(attribute, ref minimum, ref maximum);
                        }
                    }

                    foreach (string route in routes)
                    {
                        yield return new RouteInformation(verb, route, method, minimum, maximum);
                    }
                }
            }
        }
    }
}
