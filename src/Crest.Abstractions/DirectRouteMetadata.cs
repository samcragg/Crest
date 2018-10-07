// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    /// <summary>
    /// Provides information about a direct route to match.
    /// </summary>
    /// <remarks>
    /// These routes will not pass through the normal route processing pipeline.
    /// </remarks>
    public sealed class DirectRouteMetadata
    {
        /// <summary>
        /// Gets or sets the method to invoke when the route is matched.
        /// </summary>
        public OverrideMethod Method { get; set; }

        /// <summary>
        /// Gets or sets the URL to match.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the HTTP verb to match.
        /// </summary>
        public string Verb { get; set; }
    }
}
