// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Core
{
    using System;
    using Crest.Core.Util;

    /// <summary>
    /// Provides custom construction for <see cref="Link"/> objects.
    /// </summary>
    public sealed class LinkBuilder
    {
        private readonly Uri hRef;
        private readonly string relationType;
        private Uri deprecation;
        private string hRefLang;
        private string name;
        private Uri profile;
        private bool templated;
        private string title;
        private string type;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinkBuilder"/> class.
        /// </summary>
        /// <param name="relationType">
        /// Determines how the link is related to the current context.
        /// </param>
        /// <param name="reference">The resource to reference.</param>
        public LinkBuilder(string relationType, Uri reference)
        {
            Check.IsNotNullOrEmpty(relationType, nameof(relationType));
            Check.IsNotNull(reference, nameof(reference));

            this.hRef = reference;
            this.relationType = relationType;
        }

        /// <summary>
        /// Builds a link that represents the information set in this instance.
        /// </summary>
        /// <returns>A new link instance.</returns>
        public Link Build()
        {
            return new Link(
                this.deprecation,
                this.hRef,
                this.hRefLang,
                this.name,
                this.profile,
                this.relationType,
                this.templated,
                this.title,
                this.type);
        }

        /// <summary>
        /// Sets a URL that provides further information about the deprecation
        /// of the link.
        /// </summary>
        /// <param name="deprecation">The link to the deprecation information.</param>
        /// <returns>A reference to this instance.</returns>
        public LinkBuilder WithDeprecation(Uri deprecation)
        {
            this.deprecation = deprecation;
            return this;
        }

        /// <summary>
        /// Sets the language of the target resource.
        /// </summary>
        /// <param name="hRefLang">The resource language.</param>
        /// <returns>A reference to this instance.</returns>
        public LinkBuilder WithHRefLang(string hRefLang)
        {
            this.hRefLang = hRefLang;
            return this;
        }

        /// <summary>
        /// Sets the name of the link, which may be used as a secondary key for
        /// links of the same relation type.
        /// </summary>
        /// <param name="name">The name of the link.</param>
        /// <returns>A reference to this instance.</returns>
        public LinkBuilder WithName(string name)
        {
            this.name = name;
            return this;
        }

        /// <summary>
        /// Sets a URI that hints about the profile of the target resource.
        /// </summary>
        /// <param name="profile">The resource profile.</param>
        /// <returns>A reference to this instance.</returns>
        public LinkBuilder WithProfile(Uri profile)
        {
            this.profile = profile;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether the link refers to a URI template
        /// or not.
        /// </summary>
        /// <param name="templated">
        /// <c>true</c> if the link URI is a template; otherwise, <c>false</c>.
        /// </param>
        /// <returns>A reference to this instance.</returns>
        public LinkBuilder WithTemplated(bool templated)
        {
            this.templated = templated;
            return this;
        }

        /// <summary>
        /// Sets the human-readable identifier for the link.
        /// </summary>
        /// <param name="title">The title for the link.</param>
        /// <returns>A reference to this instance.</returns>
        public LinkBuilder WithTitle(string title)
        {
            this.title = title;
            return this;
        }

        /// <summary>
        /// Sets the media type expected when dereferencing the target resource.
        /// </summary>
        /// <param name="type">The media type.</param>
        /// <returns>A reference to this instance.</returns>
        public LinkBuilder WithType(string type)
        {
            this.type = type;
            return this;
        }
    }
}
