// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Core
{
    using System;

    /// <summary>
    /// Allows the availability of a method to be limited within a version range.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class VersionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionAttribute" /> class.
        /// </summary>
        /// <param name="from">The first version the method is available.</param>
        public VersionAttribute(int from)
            : this(from, int.MaxValue)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VersionAttribute" /> class.
        /// </summary>
        /// <param name="from">The first version the method is available.</param>
        /// <param name="to">The last version the method is available.</param>
        public VersionAttribute(int from, int to)
        {
            this.From = from;
            this.To = to;
        }

        /// <summary>
        /// Gets the first version the method is available from.
        /// </summary>
        public int From { get; }

        /// <summary>
        /// Gets the last version the method is available to.
        /// </summary>
        public int To { get; }
    }
}
