// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using Crest.Core;

    /// <summary>
    /// Represents the customizable options used for hosting.
    /// </summary>
    [Configuration]
    internal sealed class HostingOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the docs pages should be
        /// provided or not.
        /// </summary>
        public bool? DisplayDocs { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the health page should be
        /// provided or not.
        /// </summary>
        public bool? DisplayHealth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the metrics pages should be
        /// provided or not.
        /// </summary>
        public bool? DisplayMetrics { get; set; }
    }
}
