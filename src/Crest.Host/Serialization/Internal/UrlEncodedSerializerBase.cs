// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Internal
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// The base class for runtime serializers that output JSON.
    /// </summary>
    public abstract class UrlEncodedSerializerBase : IClassSerializer<byte[]>
    {
        private readonly UrlEncodedSerializerBase parent;
        private readonly UrlEncodedStreamReader reader;
        private readonly UrlEncodedStreamWriter writer;
        private int currentIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlEncodedSerializerBase"/> class.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="mode">The serialization mode.</param>
        protected UrlEncodedSerializerBase(Stream stream, SerializationMode mode)
        {
            if (mode == SerializationMode.Deserialize)
            {
                this.reader = new UrlEncodedStreamReader(stream);
            }
            else
            {
                this.writer = new UrlEncodedStreamWriter(stream);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlEncodedSerializerBase"/> class.
        /// </summary>
        /// <param name="parent">The serializer this instance belongs to.</param>
        protected UrlEncodedSerializerBase(UrlEncodedSerializerBase parent)
        {
            this.parent = parent;
            this.reader = parent.reader;
            this.writer = parent.writer;
        }

        /// <summary>
        /// Gets a value indicating whether the generated classes should output
        /// the names for <c>enum</c> values or not.
        /// </summary>
        /// <remarks>
        /// Always returns <c>true</c>, therefore, the generated data will use
        /// names for the enumeration values.
        /// </remarks>
        public static bool OutputEnumNames => true;

        /// <inheritdoc />
        public ValueReader Reader => this.reader;

        /// <inheritdoc />
        public ValueWriter Writer => this.writer;

        /// <summary>
        /// Gets the metadata for the specified property.
        /// </summary>
        /// <param name="property">The property information.</param>
        /// <returns>The metadata to store for the property.</returns>
        public static byte[] GetMetadata(PropertyInfo property)
        {
            DisplayNameAttribute displayName =
                property.GetCustomAttribute<DisplayNameAttribute>();

            string name = displayName?.DisplayName ?? property.Name;
            return UrlEncodeString(name);
        }

        /// <inheritdoc />
        public void BeginRead(byte[] metadata)
        {
        }

        /// <inheritdoc />
        public void BeginWrite(byte[] metadata)
        {
        }

        /// <inheritdoc />
        public void EndRead()
        {
        }

        /// <inheritdoc />
        public void EndWrite()
        {
        }

        /// <inheritdoc />
        public void Flush()
        {
            this.writer.Flush();
        }

        /// <inheritdoc />
        public bool ReadBeginArray(Type elementType)
        {
            if (this.reader.CurrentArrayIndex < 0)
            {
                return false;
            }
            else
            {
                this.reader.MoveToChildren();
                return true;
            }
        }

        /// <inheritdoc />
        public bool ReadElementSeparator()
        {
            this.reader.MoveToParent();
            if (this.reader.MoveToNextSibling())
            {
                this.reader.MoveToChildren();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc />
        public void ReadEndArray()
        {
        }

        /// <inheritdoc />
        public void WriteBeginArray(Type elementType, int size)
        {
            this.currentIndex = 0;
            this.writer.PushKeyPart(0);
        }

        /// <inheritdoc />
        public void WriteBeginClass(byte[] metadata)
        {
        }

        /// <inheritdoc />
        public void WriteBeginProperty(byte[] propertyMetadata)
        {
            this.writer.PushKeyPart(propertyMetadata);
        }

        /// <inheritdoc />
        public void WriteElementSeparator()
        {
            this.currentIndex++;

            // Replace the old array index with the new one
            this.writer.PopKeyPart();
            this.writer.PushKeyPart(this.currentIndex);
        }

        /// <inheritdoc />
        public void WriteEndArray()
        {
            this.writer.PopKeyPart();
        }

        /// <inheritdoc />
        public void WriteEndClass()
        {
        }

        /// <inheritdoc />
        public void WriteEndProperty()
        {
            this.writer.PopKeyPart();
        }

        private static byte[] UrlEncodeString(string value)
        {
            var bytes = new List<byte>(value.Length * UrlEncodedStreamWriter.MaxBytesPerCharacter);
            var buffer = new byte[UrlEncodedStreamWriter.MaxBytesPerCharacter];
            for (int i = 0; i < value.Length; i++)
            {
                int length = UrlEncodedStreamWriter.AppendChar(value, ref i, buffer, 0);
                for (int j = 0; j < length; j++)
                {
                    bytes.Add(buffer[j]);
                }
            }

            return bytes.ToArray();
        }
    }
}
