// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Xml;
    using Crest.Host.Conversion;
    using Crest.Host.Serialization.Internal;
    using SCM = System.ComponentModel;

    /// <summary>
    /// Used to output XML primitive values.
    /// </summary>
    internal sealed partial class XmlStreamWriter : IStreamWriter
    {
        private const int PrimitiveBufferLength = 64;
        private const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        private static readonly XmlWriterSettings DefaultWriterSettings = CreateXmlWriterSettings();

        private readonly byte[] byteBuffer = new byte[PrimitiveBufferLength];
        private readonly char[] charBuffer = new char[PrimitiveBufferLength];
        private readonly XmlWriter writer;
        private int depth;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlStreamWriter"/> class.
        /// </summary>
        /// <param name="stream">The stream to output the values to.</param>
        public XmlStreamWriter(Stream stream)
        {
            this.writer = XmlWriter.Create(stream, DefaultWriterSettings);
            this.writer.WriteRaw(@"<?xml version=""1.0"" encoding=""utf-8""?>");
        }

        /// <summary>
        /// Gets the nesting level of the element in the XML.
        /// </summary>
        public int Depth => this.depth;

        /// <inheritdoc />
        public void Flush()
        {
            this.writer.Flush();
        }

        /// <inheritdoc />
        public void WriteBoolean(bool value)
        {
            this.writer.WriteRaw(value ? "true" : "false");
        }

        /// <inheritdoc />
        public void WriteByte(byte value)
        {
            this.WriteUInt64(value);
        }

        /// <inheritdoc />
        public void WriteChar(char value)
        {
            this.charBuffer[0] = value;
            this.writer.WriteChars(this.charBuffer, 0, 1);
        }

        /// <inheritdoc />
        public void WriteDateTime(DateTime value)
        {
            int length = DateTimeConverter.WriteDateTime(this.byteBuffer, 0, value);
            this.WriteByteBuffer(length);
        }

        /// <inheritdoc />
        public void WriteDecimal(decimal value)
        {
            string text = value.ToString("G", NumberFormatInfo.InvariantInfo);
            this.writer.WriteRaw(text);
        }

        /// <inheritdoc />
        public void WriteDouble(double value)
        {
            string text = value.ToString("G", NumberFormatInfo.InvariantInfo);
            this.writer.WriteRaw(text);
        }

        /// <summary>
        /// Closes one element and pops the corresponding namespace scope.
        /// </summary>
        public void WriteEndElement()
        {
            this.writer.WriteEndElement();
            this.depth--;
        }

        /// <inheritdoc />
        public void WriteGuid(Guid value)
        {
            int length = GuidConverter.WriteGuid(this.byteBuffer, 0, value);
            this.WriteByteBuffer(length);
        }

        /// <inheritdoc />
        public void WriteInt16(short value)
        {
            this.WriteInt64(value);
        }

        /// <inheritdoc />
        public void WriteInt32(int value)
        {
            this.WriteInt64(value);
        }

        /// <inheritdoc />
        public void WriteInt64(long value)
        {
            int length = IntegerConverter.WriteInt64(this.byteBuffer, 0, value);
            this.WriteByteBuffer(length);
        }

        /// <inheritdoc />
        public void WriteNull()
        {
            this.writer.WriteStartAttribute("i", "nil", XmlSchemaNamespace);
            this.writer.WriteRaw("true");
            this.writer.WriteEndAttribute();
        }

        /// <inheritdoc />
        public void WriteObject(object value)
        {
            SCM.TypeConverter converter = SCM.TypeDescriptor.GetConverter(value);
            string converted = converter.ConvertToInvariantString(value);
            this.writer.WriteString(converted);
        }

        /// <inheritdoc />
        public void WriteSByte(sbyte value)
        {
            this.WriteInt64(value);
        }

        /// <inheritdoc />
        public void WriteSingle(float value)
        {
            string text = value.ToString("G", NumberFormatInfo.InvariantInfo);
            this.writer.WriteRaw(text);
        }

        /// <summary>
        /// Writes out a start tag with the specified local name.
        /// </summary>
        /// <param name="name">The local name of the element.</param>
        public void WriteStartElement(string name)
        {
            this.writer.WriteStartElement(null, name, null);

            // We need to add this namespace to the root element to allow for
            // writing null values (i:nil="true")
            if (this.depth++ == 0)
            {
                this.writer.WriteStartAttribute("xmlns", "i", string.Empty);
                this.writer.WriteRaw(XmlSchemaNamespace);
                this.writer.WriteEndAttribute();
            }
        }

        /// <inheritdoc />
        public void WriteString(string value)
        {
            this.writer.WriteString(value);
        }

        /// <inheritdoc />
        public void WriteTimeSpan(TimeSpan value)
        {
            int length = TimeSpanConverter.WriteTimeSpan(this.byteBuffer, 0, value);
            this.WriteByteBuffer(length);
        }

        /// <inheritdoc />
        public void WriteUInt16(ushort value)
        {
            this.WriteUInt64(value);
        }

        /// <inheritdoc />
        public void WriteUInt32(uint value)
        {
            this.WriteUInt64(value);
        }

        /// <inheritdoc />
        public void WriteUInt64(ulong value)
        {
            int length = IntegerConverter.WriteUInt64(this.byteBuffer, 0, value);
            this.WriteByteBuffer(length);
        }

        private static XmlWriterSettings CreateXmlWriterSettings()
        {
            return new XmlWriterSettings
            {
                CheckCharacters = false,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                NewLineHandling = NewLineHandling.None,
                OmitXmlDeclaration = true
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteByteBuffer(int length)
        {
            for (int i = 0; i < length; i++)
            {
                this.charBuffer[i] = (char)this.byteBuffer[i];
            }

            this.writer.WriteRaw(this.charBuffer, 0, length);
        }
    }
}
