// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains information about the documentation for a method.
    /// </summary>
    internal sealed class MethodDescription
    {
        /// <summary>
        /// Gets the documentation for the parameters of the method.
        /// </summary>
        public Dictionary<string, string> Parameters { get; }
            = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Gets or sets the remarks for the method.
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// Gets or sets the documentation on the returned data.
        /// </summary>
        public string Returns { get; set; }

        /// <summary>
        /// Gets or sets the summary for the method.
        /// </summary>
        public string Summary { get; set; }
    }
}
