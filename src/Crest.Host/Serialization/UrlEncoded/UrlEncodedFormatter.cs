// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.UrlEncoded
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// The base class for runtime serializers that output JSON.
    /// </summary>
    public class UrlEncodedFormatter : IFormatter
    {
        private readonly UrlEncodedStreamReader reader;
        private readonly UrlEncodedStreamWriter writer;
        private int currentIndex;
        private int propertyIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlEncodedFormatter"/> class.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="mode">The serialization mode.</param>
        public UrlEncodedFormatter(Stream stream, SerializationMode mode)
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
        /// Initializes a new instance of the <see cref="UrlEncodedFormatter"/> class.
        /// </summary>
        /// <param name="parent">The serializer this instance belongs to.</param>
        protected UrlEncodedFormatter(UrlEncodedFormatter parent)
        {
            this.reader = parent.reader;
            this.writer = parent.writer;
        }

        /// <inheritdoc />
        public bool EnumsAsIntegers => false;

        /// <inheritdoc />
        public ValueReader Reader => this.reader;

        /// <inheritdoc />
        public ValueWriter Writer => this.writer;

        /// <summary>
        /// Gets the metadata for the specified property.
        /// </summary>
        /// <param name="property">The property information.</param>
        /// <returns>The metadata to store for the property.</returns>
        public static object GetMetadata(PropertyInfo property)
        {
            DisplayNameAttribute displayName =
                property.GetCustomAttribute<DisplayNameAttribute>();

            string name = displayName?.DisplayName ?? property.Name;
            return UrlEncodeString(name);
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
        public void ReadBeginClass(object metadata)
        {
            this.propertyIndex = 0;
        }

        /// <inheritdoc />
        public void ReadBeginClass(string className)
        {
            this.ReadBeginClass((byte[])null);
        }

        /// <inheritdoc />
        public void ReadBeginPrimitive(object metadata)
        {
            // We don't need to do anything
        }

        /// <inheritdoc />
        public string ReadBeginProperty()
        {
            if ((this.propertyIndex > 0) &&
                !this.reader.MoveToNextSibling())
            {
                return null;
            }

            this.propertyIndex++;

            string part = this.reader.CurrentPart;
            this.reader.MoveToChildren();
            return part;
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
            // We don't need to do anything
        }

        /// <inheritdoc />
        public void ReadEndClass()
        {
            // We don't need to do anything
        }

        /// <inheritdoc />
        public void ReadEndPrimitive()
        {
            // We don't need to do anything
        }

        /// <inheritdoc />
        public void ReadEndProperty()
        {
            this.reader.MoveToParent();
        }

        /// <inheritdoc />
        public void WriteBeginArray(Type elementType, int size)
        {
            this.currentIndex = 0;
            this.writer.PushKeyPart(0);
        }

        /// <inheritdoc />
        public void WriteBeginClass(object metadata)
        {
            // We don't need to do anything
        }

        /// <inheritdoc />
        public void WriteBeginClass(string className)
        {
            // We don't need to do anything
        }

        /// <inheritdoc />
        public void WriteBeginPrimitive(object metadata)
        {
            // We don't need to do anything
        }

        /// <inheritdoc />
        public void WriteBeginProperty(object metadata)
        {
            this.writer.PushKeyPart((byte[])metadata);
        }

        /// <inheritdoc />
        public void WriteBeginProperty(string propertyName)
        {
            this.writer.PushKeyPart(UrlEncodeString(propertyName));
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
            // We don't need to do anything
        }

        /// <inheritdoc />
        public void WriteEndPrimitive()
        {
            // We don't need to do anything
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
