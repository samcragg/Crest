// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    /// <summary>
    /// Contains registrations for Link Relation Types from RFC 5988.
    /// </summary>
    public static class Relations
    {
        /// <summary>
        /// Refers to the furthest preceding resource in a series of resources.
        /// </summary>
        public const string First = "first";

        /// <summary>
        /// Refers to the furthest following resource in a series of resources.
        /// </summary>
        public const string Last = "last";

        /// <summary>
        /// Refers to the next resource in a ordered series of resources.
        /// </summary>
        public const string Next = "next";

        /// <summary>
        /// Refers to the previous resource in an ordered series of resources.
        /// </summary>
        public const string Previous = "prev";

        /// <summary>
        /// Conveys an identifier for the link's context.
        /// </summary>
        public const string Self = "self";
    }
}
