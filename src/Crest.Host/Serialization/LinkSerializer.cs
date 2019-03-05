// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using Crest.Core;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Allows the serialization of <see cref="Link"/>.
    /// </summary>
    internal sealed class LinkSerializer : ISerializer<Link>
    {
        /// <inheritdoc />
        public void Write(IClassWriter writer, Link instance)
        {
            SerializeLink(writer, instance);
        }

        /// <inheritdoc />
        Link ISerializer<Link>.Read(IClassReader reader)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Serializes a <see cref="Link"/> to the specified <see cref="IClassWriter"/>.
        /// </summary>
        /// <param name="writer">Where to write the link to.</param>
        /// <param name="link">The value to write.</param>
        internal static void SerializeLink(IClassWriter writer, Link link)
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
