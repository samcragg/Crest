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
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Used to format the output as JSON.
    /// </summary>
    internal class JsonFormatter : IFormatter, IDisposable
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

        /// <inheritdoc />
        public bool EnumsAsIntegers => true;

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
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
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
        public void ReadBeginClass(object metadata)
        {
            this.reader.ExpectToken('{');
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
        public void ReadEndPrimitive()
        {
            // We don't need to do anything
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
        public void WriteBeginClass(object metadata)
        {
            this.writer.AppendByte((byte)'{');
            this.hasPropertyWritten = false;
        }

        /// <inheritdoc />
        public void WriteBeginClass(string className)
        {
            this.WriteBeginClass((object)null);
        }

        /// <inheritdoc />
        public void WriteBeginPrimitive(object metadata)
        {
            // We don't need to do anything
        }

        /// <inheritdoc />
        public void WriteBeginProperty(object metadata)
        {
            this.WritePropertySeparator();
            this.writer.AppendBytes((byte[])metadata);
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
        public void WriteEndPrimitive()
        {
            // We don't need to do anything
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
