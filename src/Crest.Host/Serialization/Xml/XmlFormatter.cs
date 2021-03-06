﻿// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Xml
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// The base class for runtime serializers that output XML.
    /// </summary>
    internal class XmlFormatter : IFormatter, IDisposable
    {
        private readonly XmlStreamReader reader;
        private readonly XmlStreamWriter writer;
        private string arrayElementName;
        private bool hasRootArrayElement;
        private ReadState readState;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFormatter"/> class.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="mode">The serialization mode.</param>
        public XmlFormatter(Stream stream, SerializationMode mode)
        {
            if (mode == SerializationMode.Deserialize)
            {
                this.reader = new XmlStreamReader(stream);
            }
            else
            {
                this.writer = new XmlStreamWriter(stream);
            }
        }

        private enum ReadState
        {
            None,
            Array,
            Object,
            Property,
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

            return XmlConvert.EncodeName(displayName?.DisplayName ?? property.Name);
        }

        /// <summary>
        /// Gets the metadata for the specified type.
        /// </summary>
        /// <param name="type">The type information.</param>
        /// <returns>The metadata to store for the type.</returns>
        public static object GetTypeMetadata(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            string name = GetPrimitiveName(type) ?? type.Name;
            return XmlConvert.EncodeName(name);
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
            // Are we just reading an array?
            if (this.readState == ReadState.None)
            {
                string name =
                    GetPrimitiveName(elementType) ??
                    XmlConvert.EncodeName(elementType.Name);

                this.ExpectStartElement("ArrayOf" + name);
            }

            this.readState = ReadState.Array;
            return this.reader.CanReadStartElement();
        }

        /// <inheritdoc />
        public void ReadBeginClass(object metadata)
        {
            this.ReadBeginClass((string)metadata);
        }

        /// <inheritdoc />
        public void ReadBeginClass(string className)
        {
            if (this.readState != ReadState.Property)
            {
                this.ExpectStartElement(className);
            }

            this.readState = ReadState.Object;
        }

        /// <inheritdoc />
        public void ReadBeginPrimitive(object metadata)
        {
            this.ReadBeginClass((string)metadata);
        }

        /// <inheritdoc />
        public string ReadBeginProperty()
        {
            if (!this.reader.CanReadStartElement())
            {
                return null;
            }
            else
            {
                this.readState = ReadState.Property;
                return this.reader.ReadStartElement();
            }
        }

        /// <inheritdoc />
        public bool ReadElementSeparator()
        {
            return this.reader.CanReadStartElement();
        }

        /// <inheritdoc />
        public void ReadEndArray()
        {
            this.reader.ReadEndElement();
            this.readState = ReadState.Array;
        }

        /// <inheritdoc />
        public void ReadEndClass()
        {
            this.reader.ReadEndElement();
            this.readState = ReadState.Object;
        }

        /// <inheritdoc />
        public void ReadEndPrimitive()
        {
            this.ReadEndClass();
        }

        /// <inheritdoc />
        public void ReadEndProperty()
        {
            if (this.readState == ReadState.Property)
            {
                this.reader.ReadEndElement();
            }

            this.readState = ReadState.Property;
        }

        /// <inheritdoc />
        public void WriteBeginArray(Type elementType, int size)
        {
            string name = GetPrimitiveName(elementType);
            this.arrayElementName = name ?? XmlConvert.EncodeName(elementType.Name);

            // We're just writing an array so need to wrap it in a root element
            if (this.writer.Depth == 0)
            {
                this.hasRootArrayElement = true;
                this.writer.WriteStartElement("ArrayOf" + this.arrayElementName);
            }

            this.writer.WriteStartElement(this.arrayElementName);
        }

        /// <inheritdoc />
        public void WriteBeginClass(object metadata)
        {
            this.WriteBeginClass((string)metadata);
        }

        /// <inheritdoc />
        public void WriteBeginClass(string className)
        {
            if (this.writer.Depth == 0)
            {
                this.writer.WriteStartElement(className);
            }
        }

        /// <inheritdoc />
        public void WriteBeginPrimitive(object metadata)
        {
            this.WriteBeginClass((string)metadata);
        }

        /// <inheritdoc />
        public void WriteBeginProperty(object metadata)
        {
            this.WriteBeginProperty((string)metadata);
        }

        /// <inheritdoc />
        public void WriteBeginProperty(string propertyName)
        {
            this.writer.WriteStartElement(propertyName);
        }

        /// <inheritdoc />
        public void WriteElementSeparator()
        {
            this.writer.WriteEndElement();
            this.writer.WriteStartElement(this.arrayElementName);
        }

        /// <inheritdoc />
        public void WriteEndArray()
        {
            this.writer.WriteEndElement();

            if ((this.writer.Depth == 1) && this.hasRootArrayElement)
            {
                this.writer.WriteEndElement();
                this.hasRootArrayElement = false;
            }
        }

        /// <inheritdoc />
        public void WriteEndClass()
        {
            if (this.writer.Depth == 1)
            {
                this.writer.WriteEndElement();
            }
        }

        /// <inheritdoc />
        public void WriteEndPrimitive()
        {
            this.WriteEndClass();
        }

        /// <inheritdoc />
        public void WriteEndProperty()
        {
            this.writer.WriteEndElement();
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
                this.reader?.Dispose();
                this.writer?.Dispose();
            }
        }

        private static string GetPrimitiveName(Type type)
        {
            // Gets the name as per http://www.w3.org/TR/xmlschema11-2/
            // Treat nullables as their underlying type
            type = Nullable.GetUnderlyingType(type) ?? type;
            switch (type.Name)
            {
                case nameof(Boolean):
                    return "boolean";

                case nameof(Byte):
                    return "unsignedByte";

                case nameof(DateTime):
                    return "dateTime";

                case nameof(Decimal):
                    return "decimal";

                case nameof(Double):
                    return "double";

                case nameof(Int16):
                    return "short";

                case nameof(Int32):
                    return "int";

                case nameof(Int64):
                    return "long";

                case nameof(SByte):
                    return "byte";

                case nameof(Single):
                    return "float";

                case nameof(String):
                    return "string";

                case nameof(TimeSpan):
                    return "duration";

                case nameof(UInt16):
                    return "unsignedShort";

                case nameof(UInt32):
                    return "unsignedInt";

                case nameof(UInt64):
                    return "unsignedLong";

                default:
                    return null;
            }
        }

        private void ExpectStartElement(string name)
        {
            string element = this.reader.ReadStartElement();
            if (!string.Equals(element, name, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException(
                    $"Expected start element to be {name} at {this.reader.GetCurrentPosition()}");
            }
        }
    }
}
