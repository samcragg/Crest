// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Core
{
    using System;
    using Crest.Core.Util;

    /// <summary>
    /// Represents a link to a resource.
    /// </summary>
    public sealed class Link : IEquatable<Link>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Link"/> class.
        /// </summary>
        /// <param name="reference">The resource reference.</param>
        public Link(Uri reference)
        {
            Check.IsNotNull(reference, nameof(reference));

            this.Reference = reference;
        }

        /// <summary>
        /// Gets the reference to the resource.
        /// </summary>
        public Uri Reference { get; }

        /// <summary>
        /// Allows the implicit conversion from a <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">The resource reference.</param>
        public static implicit operator Link(Uri uri)
        {
            return new Link(uri);
        }

        /// <summary>
        /// Creates a Link from a given Uri.
        /// </summary>
        /// <param name="uri">The resource reference.</param>
        /// <returns>The created instance.</returns>
        public static Link FromUri(Uri uri)
        {
            return uri;
        }

        /// <inheritdoc />
        public bool Equals(Link other)
        {
            if (object.Equals(other, null))
            {
                return false;
            }

            return this.Reference.Equals(other.Reference);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return this.Equals(obj as Link);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.Reference.GetHashCode();
        }
    }
}
