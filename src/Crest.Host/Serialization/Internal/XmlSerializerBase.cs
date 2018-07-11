// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Internal
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;
    using System.Xml;

    /// <summary>
    /// The base class for runtime serializers that output XML.
    /// </summary>
    public abstract class XmlSerializerBase : IClassSerializer<string>, IDisposable
    {
        private readonly XmlStreamReader reader;
        private readonly XmlStreamWriter writer;
        private string arrayElementName;
        private bool hasRootArrayElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlSerializerBase"/> class.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="mode">The serialization mode.</param>
        protected XmlSerializerBase(Stream stream, SerializationMode mode)
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

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlSerializerBase"/> class.
        /// </summary>
        /// <param name="parent">The serializer this instance belongs to.</param>
        protected XmlSerializerBase(XmlSerializerBase parent)
        {
            this.reader = parent.reader;
            this.writer = parent.writer;
        }

        /// <summary>
        /// Gets a value indicating whether the generated classes should output
        /// the names for <c>enum</c> values or not.
        /// </summary>
        /// <remarks>
        /// Always returns <c>true</c>, therefore, the generated XML will use
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
        public static string GetMetadata(PropertyInfo property)
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
        public static string GetTypeMetadata(Type type)
        {
            string name = GetPrimitiveName(type) ?? type.Name;
            return XmlConvert.EncodeName(name);
        }

        /// <inheritdoc />
        public void BeginRead(string metadata)
        {
            this.ExpectStartElement(metadata);
        }

        /// <inheritdoc />
        public void BeginWrite(string metadata)
        {
            this.writer.WriteStartElement(metadata);
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
            this.reader.ReadEndElement();
        }

        /// <inheritdoc />
        public void EndWrite()
        {
            this.writer.WriteEndElement();
        }

        /// <inheritdoc />
        public void Flush()
        {
            this.writer.Flush();
        }

        /// <inheritdoc />
        public bool ReadBeginArray(Type elementType)
        {
            string name =
                GetPrimitiveName(elementType) ??
                XmlConvert.EncodeName(elementType.Name);

            // Are we just reading an array?
            if (this.reader.Depth == 0)
            {
                this.ExpectStartElement("ArrayOf" + name);
                this.hasRootArrayElement = true;
            }

            if (this.reader.CanReadStartElement())
            {
                this.ExpectStartElement(name);
                this.arrayElementName = name;
                return true;
            }
            else
            {
                this.hasRootArrayElement = false;
                return false;
            }
        }

        /// <inheritdoc />
        public void ReadBeginClass(string metadata)
        {
            if (this.reader.Depth == 0)
            {
                this.ExpectStartElement(metadata);
            }
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
                return this.reader.ReadStartElement();
            }
        }

        /// <inheritdoc />
        public bool ReadElementSeparator()
        {
            this.reader.ReadEndElement();
            if (this.reader.CanReadStartElement())
            {
                this.ExpectStartElement(this.arrayElementName);
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
            if ((this.reader.Depth == 0) && this.hasRootArrayElement)
            {
                this.reader.ReadEndElement();
                this.hasRootArrayElement = false;
            }
        }

        /// <inheritdoc />
        public void ReadEndClass()
        {
            if (this.reader.Depth == 0)
            {
                this.reader.ReadEndElement();
            }
        }

        /// <inheritdoc />
        public void ReadEndProperty()
        {
            this.reader.ReadEndElement();
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
        public void WriteBeginClass(string metadata)
        {
            if (this.writer.Depth == 0)
            {
                this.writer.WriteStartElement(metadata);
            }
        }

        /// <inheritdoc />
        public void WriteBeginProperty(string propertyMetadata)
        {
            this.writer.WriteStartElement(propertyMetadata);
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
                this.reader.Dispose();
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
