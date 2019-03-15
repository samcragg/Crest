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
    internal sealed class LinkCollectionSerializer : ISerializer<LinkCollection>
    {
        private readonly ISerializer<Link> linkSerializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinkCollectionSerializer"/> class.
        /// </summary>
        /// <param name="linkSerializer">Used to serializer the links.</param>
        public LinkCollectionSerializer(ISerializer<Link> linkSerializer)
        {
            this.linkSerializer = linkSerializer;
        }

        /// <inheritdoc />
        public void Write(IClassWriter writer, LinkCollection instance)
        {
            writer.WriteBeginClass(nameof(LinkCollection));
            foreach (IGrouping<string, Link> group in (ILookup<string, Link>)instance)
            {
                writer.WriteBeginProperty(group.Key);
                this.SerializeLinks(writer, (IReadOnlyCollection<Link>)group);
                writer.WriteEndProperty();
            }

            writer.WriteEndClass();
        }

        /// <inheritdoc />
        LinkCollection ISerializer<LinkCollection>.Read(IClassReader reader)
        {
            throw new NotSupportedException();
        }

        private void SerializeLinks(IClassWriter writer, IReadOnlyCollection<Link> links)
        {
            int count = links.Count;
            if (count == 1)
            {
                this.linkSerializer.Write(writer, links.FirstOrDefault());
            }
            else
            {
                writer.WriteBeginArray(typeof(Link), count);
                foreach (Link link in links)
                {
                    this.linkSerializer.Write(writer, link);

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
    }
}
