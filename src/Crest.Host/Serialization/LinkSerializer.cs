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
    internal sealed class LinkSerializer : ICustomSerializer<LinkCollection>
    {
        /// <inheritdoc />
        public void Write(IClassWriter writer, LinkCollection instance)
        {
            writer.WriteBeginClass(nameof(LinkCollection));
            foreach (IGrouping<string, Link> group in (ILookup<string, Link>)instance)
            {
                writer.WriteBeginProperty(group.Key);
                SerializeLinks(writer, (IReadOnlyCollection<Link>)group);
                writer.WriteEndProperty();
            }

            writer.WriteEndClass();
        }

        /// <inheritdoc />
        LinkCollection ICustomSerializer<LinkCollection>.Read(IClassReader reader)
        {
            throw new NotSupportedException();
        }

        private static void SerializeLink(IClassWriter writer, Link link)
        {
            writer.WriteBeginClass(nameof(Link));
            SerializeNonNullProperty(writer, nameof(Link.HRef), link.HRef);
            if (link.Templated)
            {
                writer.WriteBeginProperty(nameof(Link.Templated));
                writer.Writer.WriteBoolean(true);
                writer.WriteEndProperty();
            }

            SerializeNonNullProperty(writer, nameof(Link.Type), link.Type);
            SerializeNonNullProperty(writer, nameof(Link.Deprecation), link.Deprecation);
            SerializeNonNullProperty(writer, nameof(Link.Name), link.Name);
            SerializeNonNullProperty(writer, nameof(Link.Profile), link.Profile);
            SerializeNonNullProperty(writer, nameof(Link.Title), link.Title);
            SerializeNonNullProperty(writer, nameof(Link.HRefLang), link.HRefLang);
            writer.WriteEndClass();
        }

        private static void SerializeLinks(IClassWriter writer, IReadOnlyCollection<Link> links)
        {
            int count = links.Count;
            if (count == 1)
            {
                SerializeLink(writer, links.FirstOrDefault());
            }
            else
            {
                writer.WriteBeginArray(typeof(Link), count);
                foreach (Link link in links)
                {
                    SerializeLink(writer, link);

                    // Are we expecting more?
                    count--;
                    if (count > 0)
                    {
                        writer.WriteElementSeparator();
                    }
                }

                writer.WriteEndArray();
            }
        }

        private static void SerializeNonNullProperty(IClassWriter writer, string propertyName, string value)
        {
            if (value != null)
            {
                writer.WriteBeginProperty(propertyName);
                writer.Writer.WriteString(value);
                writer.WriteEndProperty();
            }
        }

        private static void SerializeNonNullProperty(IClassWriter writer, string propertyName, Uri value)
        {
            if (value != null)
            {
                writer.WriteBeginProperty(propertyName);
                writer.Writer.WriteUri(value);
                writer.WriteEndProperty();
            }
        }
    }
}
