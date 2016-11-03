// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Core
{
    using System;

    /// <summary>
    /// Marks a method as handling HTTP PUT requests to the specified route.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class PutAttribute : RouteAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PutAttribute"/> class.
        /// </summary>
        /// <param name="route">Describes the route URL to match.</param>
        public PutAttribute(string route)
            : base(route)
        {
        }

        /// <inheritdoc />
        public override string Verb
        {
            get { return "PUT"; }
        }
    }
}
