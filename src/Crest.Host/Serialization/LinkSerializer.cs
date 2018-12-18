// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Core;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Allows the serialization of <see cref="LinkCollection"/>.
    /// </summary>
    internal sealed class LinkSerializer
    {
        private readonly IClassWriter writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinkSerializer"/> class.
        /// </summary>
        /// <param name="writer">Used to write the object to.</param>
        public LinkSerializer(IClassWriter writer)
        {
            this.writer = writer;
        }

        /// <summary>
        /// Serializes the specified instance.
        /// </summary>
        /// <param name="links">The object to serialize.</param>
        public void Serialize(LinkCollection links)
        {
            this.writer.WriteBeginClass(nameof(LinkCollection));
            foreach (IGrouping<string, Link> group in (ILookup<string, Link>)links)
            {
                this.writer.WriteBeginProperty(group.Key);
                this.SerializeLinks((IReadOnlyCollection<Link>)group);
                this.writer.WriteEndProperty();
            }

            this.writer.WriteEndClass();
        }

        private void SerializeLinks(IReadOnlyCollection<Link> links)
        {
            int count = links.Count;
            if (count == 1)
            {
                this.SerializeLink(links.FirstOrDefault());
            }
            else
            {
                this.writer.WriteBeginArray(typeof(Link), count);
                foreach (Link link in links)
                {
                    this.SerializeLink(link);

                    // Are we expecting more?
                    count--;
                    if (count > 0)
                    {
                        this.writer.WriteElementSeparator();
                    }
                }

                this.writer.WriteEndArray();
            }
        }

        private void SerializeLink(Link link)
        {
            this.SerializeNonNullProperty(nameof(Link.HRef), link.HRef);
            if (link.Templated)
            {
                this.writer.WriteBeginProperty(nameof(Link.Templated));
                this.writer.Writer.WriteBoolean(true);
                this.writer.WriteEndProperty();
            }

            this.SerializeNonNullProperty(nameof(Link.Type), link.Type);
            this.SerializeNonNullProperty(nameof(Link.Deprecation), link.Deprecation);
            this.SerializeNonNullProperty(nameof(Link.Name), link.Name);
            this.SerializeNonNullProperty(nameof(Link.Profile), link.Profile);
            this.SerializeNonNullProperty(nameof(Link.Title), link.Title);
            this.SerializeNonNullProperty(nameof(Link.HRefLang), link.HRefLang);
        }

        private void SerializeNonNullProperty(string propertyName, string value)
        {
            if (value != null)
            {
                this.writer.WriteBeginProperty(propertyName);
                this.writer.Writer.WriteString(value);
                this.writer.WriteEndProperty();
            }
        }

        private void SerializeNonNullProperty(string propertyName, Uri value)
        {
            if (value != null)
            {
                this.writer.WriteBeginProperty(propertyName);
                this.writer.Writer.WriteUri(value);
                this.writer.WriteEndProperty();
            }
        }
    }
}
