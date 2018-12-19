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
        /// <param name="relationType">
        /// Determines how the link is related to the current context.
        /// </param>
        /// <param name="reference">The resource to reference.</param>
        public Link(string relationType, Uri reference)
        {
            Check.IsNotNullOrEmpty(relationType, nameof(relationType));
            Check.IsNotNull(reference, nameof(reference));

            this.HRef = reference;
            this.RelationType = relationType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Link"/> class.
        /// </summary>
        /// <param name="deprecation">The value for <see cref="Deprecation"/>.</param>
        /// <param name="hRef">The value for <see cref="HRef"/>.</param>
        /// <param name="hRefLang">The value for <see cref="HRefLang"/>.</param>
        /// <param name="name">The value for <see cref="Name"/>.</param>
        /// <param name="profile">The value for <see cref="Profile"/>.</param>
        /// <param name="relationType">The value for <see cref="RelationType"/>.</param>
        /// <param name="templated">The value for <see cref="Templated"/>.</param>
        /// <param name="title">The value for <see cref="Title"/>.</param>
        /// <param name="type">The value for <see cref="Type"/>.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "brain-overload",
            "S107",
            Justification = "Internal method used by the builder")]
        internal Link(
            Uri deprecation,
            Uri hRef,
            string hRefLang,
            string name,
            Uri profile,
            string relationType,
            bool templated,
            string title,
            string type)
        {
            this.Deprecation = deprecation;
            this.HRef = hRef;
            this.HRefLang = hRefLang;
            this.Name = name;
            this.Profile = profile;
            this.RelationType = relationType;
            this.Templated = templated;
            this.Title = title;
            this.Type = type;
        }

        /// <summary>
        /// Gets a URL that provides further information about the deprecation
        /// of the link.
        /// </summary>
        public Uri Deprecation { get; }

        /// <summary>
        /// Gets the reference to the resource.
        /// </summary>
        public Uri HRef { get; }

        /// <summary>
        /// Gets the language of the target resource.
        /// </summary>
        public string HRefLang { get; }

        /// <summary>
        /// Gets the name of the link, which may be used as a secondary key for
        /// links of the same relation type.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a URI that hints about the profile of the target resource.
        /// </summary>
        public Uri Profile { get; }

        /// <summary>
        /// Gets the relationship of the link to the current context.
        /// </summary>
        public string RelationType { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="HRef"/> property
        /// refers to URI template or not.
        /// </summary>
        public bool Templated { get; }

        /// <summary>
        /// Gets the human-readable identifier for the link.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the media type expected when dereferencing the target resource.
        /// </summary>
        public string Type { get; }

        /// <inheritdoc />
        public bool Equals(Link other)
        {
            if (object.Equals(other, null))
            {
                return false;
            }

            return this.HRef.Equals(other.HRef);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return this.Equals(obj as Link);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.HRef.GetHashCode();
        }
    }
}
