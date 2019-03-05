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
        LinkCollection ISerializer<LinkCollection>.Read(IClassReader reader)
        {
            throw new NotSupportedException();
        }

        private static void SerializeLinks(IClassWriter writer, IReadOnlyCollection<Link> links)
        {
            int count = links.Count;
            if (count == 1)
            {
                LinkSerializer.SerializeLink(writer, links.FirstOrDefault());
            }
            else
            {
                writer.WriteBeginArray(typeof(Link), count);
                foreach (Link link in links)
                {
                    LinkSerializer.SerializeLink(writer, link);

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
