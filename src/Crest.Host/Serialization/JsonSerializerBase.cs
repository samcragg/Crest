// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// The base class for runtime serializers that output JSON.
    /// </summary>
    public abstract class JsonSerializerBase : IClassSerializer<byte[]>
    {
        private readonly JsonStreamWriter writer;
        private bool hasPropertyWritten;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializerBase"/> class.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        protected JsonSerializerBase(Stream stream)
        {
            this.writer = new JsonStreamWriter(stream);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializerBase"/> class.
        /// </summary>
        /// <param name="parent">The serializer this instance belongs to.</param>
        protected JsonSerializerBase(JsonSerializerBase parent)
        {
            this.writer = parent.writer;
        }

        /// <summary>
        /// Gets a value indicating whether the generated classes should output
        /// the names for <c>enum</c> values or not.
        /// </summary>
        /// <remarks>
        /// Always returns <c>false</c>, therefore, the generated JSON will use
        /// numbers for the enumeration values.
        /// </remarks>
        public static bool OutputEnumNames => false;

        /// <inheritdoc />
        [CLSCompliant(false)]
        public IStreamWriter Writer => this.writer;

        /// <summary>
        /// Gets the metadata for the specified property.
        /// </summary>
        /// <param name="property">The property information.</param>
        /// <returns>The metadata to store for the property.</returns>
        public static byte[] GetMetadata(PropertyInfo property)
        {
            DisplayNameAttribute displayName =
                property.GetCustomAttribute<DisplayNameAttribute>();

            string name = (displayName != null) ?
                displayName.DisplayName :
                MakeCamelCase(property.Name);

            // +3 for the enclosing characters (i.e. we're returning "...":)
            var bytes = new List<byte>((name.Length * JsonStringEncoding.MaxBytesPerCharacter) + 3);
            IEnumerable<byte> nameBytes = EncodeJsonString(name);

            bytes.Add((byte)'"');
            bytes.AddRange(nameBytes);
            bytes.Add((byte)'"');
            bytes.Add((byte)':');

            return bytes.ToArray();
        }

        /// <inheritdoc />
        public void BeginWrite(byte[] metadata)
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
        public void WriteBeginArray(Type elementType, int size)
        {
            this.writer.AppendByte((byte)'[');
        }

        /// <inheritdoc />
        public void WriteBeginClass(byte[] metadata)
        {
            this.writer.AppendByte((byte)'{');
            this.hasPropertyWritten = false;
        }

        /// <inheritdoc />
        public void WriteBeginProperty(byte[] propertyMetadata)
        {
            if (this.hasPropertyWritten)
            {
                this.writer.AppendByte((byte)',');
            }

            this.hasPropertyWritten = true;
            this.writer.AppendBytes(propertyMetadata);
        }

        /// <inheritdoc />
        public void WriteElementSeparator()
        {
            this.writer.AppendByte((byte)',');
        }

        /// <inheritdoc />
        public void WriteEndArray()
        {
            this.writer.AppendByte((byte)']');
        }

        /// <inheritdoc />
        public void WriteEndClass()
        {
            this.writer.AppendByte((byte)'}');
        }

        /// <inheritdoc />
        public void WriteEndProperty()
        {
        }

        private static IEnumerable<byte> EncodeJsonString(string name)
        {
            byte[] buffer = new byte[JsonStringEncoding.MaxBytesPerCharacter];
            for (int i = 0; i < name.Length; i++)
            {
                int offset = 0;
                JsonStringEncoding.AppendChar(name, ref i, buffer, ref offset);

                for (int j = 0; j < offset; j++)
                {
                    yield return buffer[j];
                }
            }
        }

        private static string MakeCamelCase(string name)
        {
            char[] chars = name.ToCharArray();
            for (int i = 0; i < chars.Length - 1; i++)
            {
                char current = chars[i];
                char next = chars[i + 1];

                // If it's an uppercase letter and either it's the first
                // character or the next character is uppercase then make it
                // lower. This allows for the following:
                // * Simple -> simple
                // * XMLData -> xmlData
                if (char.IsUpper(current) && ((i == 0) || char.IsUpper(next)))
                {
                    chars[i] = char.ToLowerInvariant(current);
                }
                else
                {
                    break;
                }
            }

            return new string(chars);
        }
    }
}
