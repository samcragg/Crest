// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Reflection;
    using Crest.Host.Routing.Captures;

    /// <summary>
    /// Represents the information for a method that handles a route.
    /// </summary>
    internal sealed class RouteMethodInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RouteMethodInfo"/> class.
        /// </summary>
        /// <param name="bodyParameterName">
        /// The name of the parameter for the request body.
        /// </param>
        /// <param name="bodyType">The type for the request body.</param>
        /// <param name="method">The method that handles the endpoint.</param>
        /// <param name="queryCaptures">
        /// The parameters that are set from the query string.
        /// </param>
        public RouteMethodInfo(
            string bodyParameterName,
            Type bodyType,
            MethodInfo method,
            QueryCapture[] queryCaptures)
        {
            this.BodyParameterName = bodyParameterName;
            this.BodyType = bodyType;
            this.Method = method;
            this.QueryCaptures = queryCaptures;
        }

        /// <summary>
        /// Gets the name of the parameter that the request body is injected in,
        /// if any.
        /// </summary>
        public string BodyParameterName { get; }

        /// <summary>
        /// Gets the type the request body should be converted to, if any.
        /// </summary>
        public Type BodyType { get; }

        /// <summary>
        /// Gets a value indicating whether the request body is captured or not.
        /// </summary>
        public bool HasBodyParameter => this.BodyParameterName != null;

        /// <summary>
        /// Gets the method that handles the endpoint.
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// Gets the parameters that are set from the query string.
        /// </summary>
        public QueryCapture[] QueryCaptures { get; }
    }
}
