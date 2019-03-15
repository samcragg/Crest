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
            writer.WriteBeginClass(nameof(Link));
            SerializeNonNullProperty(writer, nameof(Link.HRef), instance.HRef);
            if (instance.Templated)
            {
                writer.WriteBeginProperty(nameof(Link.Templated));
                writer.Writer.WriteBoolean(true);
                writer.WriteEndProperty();
            }

            SerializeNonNullProperty(writer, nameof(Link.Type), instance.Type);
            SerializeNonNullProperty(writer, nameof(Link.Deprecation), instance.Deprecation);
            SerializeNonNullProperty(writer, nameof(Link.Name), instance.Name);
            SerializeNonNullProperty(writer, nameof(Link.Profile), instance.Profile);
            SerializeNonNullProperty(writer, nameof(Link.Title), instance.Title);
            SerializeNonNullProperty(writer, nameof(Link.HRefLang), instance.HRefLang);
            writer.WriteEndClass();
        }

        /// <inheritdoc />
        Link ISerializer<Link>.Read(IClassReader reader)
        {
            throw new NotSupportedException();
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
