// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Json
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// The base class for runtime serializers that output JSON.
    /// </summary>
    public class JsonFormatter : IClassSerializer<byte[]>, IDisposable
    {
        private readonly JsonStreamReader reader;
        private readonly JsonStreamWriter writer;
        private bool hasPropertyWritten;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonFormatter"/> class.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="mode">The serialization mode.</param>
        public JsonFormatter(Stream stream, SerializationMode mode)
        {
            if (mode == SerializationMode.Deserialize)
            {
                this.reader = new JsonStreamReader(stream);
            }
            else
            {
                this.writer = new JsonStreamWriter(stream);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonFormatter"/> class.
        /// </summary>
        /// <param name="parent">The serializer this instance belongs to.</param>
        protected JsonFormatter(JsonFormatter parent)
        {
            this.reader = parent.reader;
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
        public void BeginRead(byte[] metadata)
        {
            // We don't need to do anything
        }

        /// <inheritdoc />
        public void BeginWrite(byte[] metadata)
        {
            // We don't need to do anything
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public void EndRead()
        {
            // We don't need to do anything
        }

        /// <inheritdoc />
        public void EndWrite()
        {
            // We don't need to do anything
        }

        /// <summary>
        /// Implementation of the <see cref="ITypeSerializer.Flush"/> method.
        /// </summary>
        public virtual void Flush()
        {
            this.writer.Flush();
        }

        /// <inheritdoc />
        public bool ReadBeginArray(Type elementType)
        {
            if (this.reader.TryReadToken('['))
            {
                // Make sure we return false for empty arrays
                return !this.reader.TryReadToken(']');
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc />
        public void ReadBeginClass(byte[] metadata)
        {
            this.reader.ExpectToken('{');
        }

        /// <inheritdoc />
        public void ReadBeginClass(string className)
        {
            this.ReadBeginClass((byte[])null);
        }

        /// <inheritdoc />
        public string ReadBeginProperty()
        {
            if (this.reader.PeekToken() != '"')
            {
                return null;
            }
            else
            {
                string name = this.reader.ReadString();
                this.reader.ExpectToken(':');
                return name;
            }
        }

        /// <inheritdoc />
        public bool ReadElementSeparator()
        {
            return this.reader.TryReadToken(',');
        }

        /// <inheritdoc />
        public void ReadEndArray()
        {
            this.reader.ExpectToken(']');
        }

        /// <inheritdoc />
        public void ReadEndClass()
        {
            this.reader.ExpectToken('}');
        }

        /// <inheritdoc />
        public void ReadEndProperty()
        {
            // This is slightly less strict and allows for trailing commas, i.e.
            // { "property":false, } is invalid JSON due to the trailing comma
            // but we allow it
            this.reader.TryReadToken(',');
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
        public void WriteBeginClass(string className)
        {
            this.WriteBeginClass((byte[])null);
        }

        /// <inheritdoc />
        public void WriteBeginProperty(byte[] propertyMetadata)
        {
            this.WritePropertySeparator();
            this.writer.AppendBytes(propertyMetadata);
        }

        /// <inheritdoc />
        public void WriteBeginProperty(string propertyName)
        {
            this.WritePropertySeparator();
            this.writer.WriteString(MakeCamelCase(propertyName));
            this.writer.AppendByte((byte)':');
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
            // We don't need to do anything
        }

        /// <summary>
        /// Called to clean up resources by the class.
        /// </summary>
        /// <param name="disposing">
        /// Indicates whether the method was invoked from the <see cref="Dispose()"/>
        /// implementation or from the finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.reader.Dispose();
            }
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

        private void WritePropertySeparator()
        {
            if (this.hasPropertyWritten)
            {
                this.writer.AppendByte((byte)',');
            }

            this.hasPropertyWritten = true;
        }
    }
}
