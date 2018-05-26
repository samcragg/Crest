// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;

    /// <content>
    /// Contains the nested <see cref="ParameterData"/> class.
    /// </content>
    internal abstract partial class UrlParser
    {
        /// <summary>
        /// Contains information about a method parameter.
        /// </summary>
        /// <remarks>
        /// Since the parser is being used at both runtime and during compile
        /// time analysis, abstract away the properties we're interested in.
        /// </remarks>
        protected class ParameterData
        {
            /// <summary>
            /// Gets or sets a value indicating whether the parameter has been
            /// marked as coming from the request body or not.
            /// </summary>
            public bool HasBodyAttribute { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the parameter is optional.
            /// </summary>
            public bool IsOptional { get; set; }

            /// <summary>
            /// Gets or sets the name of the parameter.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the type of the parameter.
            /// </summary>
            public Type ParameterType { get; set; }
        }
    }
}
