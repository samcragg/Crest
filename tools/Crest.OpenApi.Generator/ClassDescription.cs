// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi.Generator
{
    /// <summary>
    /// Contains information about the documentation for a type.
    /// </summary>
    internal sealed class ClassDescription
    {
        /// <summary>
        /// Gets or sets the remarks for the class.
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// Gets or sets the summary for the class.
        /// </summary>
        public string Summary { get; set; }
    }
}
