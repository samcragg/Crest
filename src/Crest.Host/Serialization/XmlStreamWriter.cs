// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Xml;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Used to output XML primitive values.
    /// </summary>
    internal sealed partial class XmlStreamWriter : ValueWriter
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
        public override void Flush()
        {
            this.writer.Flush();
        }

        /// <inheritdoc />
        public override void WriteBoolean(bool value)
        {
            this.writer.WriteRaw(value ? "true" : "false");
        }

        /// <inheritdoc />
        public override void WriteChar(char value)
        {
            this.charBuffer[0] = value;
            this.writer.WriteChars(this.charBuffer, 0, 1);
        }

        /// <inheritdoc />
        public override void WriteNull()
        {
            this.writer.WriteStartAttribute("i", "nil", XmlSchemaNamespace);
            this.writer.WriteRaw("true");
            this.writer.WriteEndAttribute();
        }

        /// <inheritdoc />
        public override void WriteString(string value)
        {
            this.writer.WriteString(value);
        }

        /// <summary>
        /// Closes one element and pops the corresponding namespace scope.
        /// </summary>
        internal void WriteEndElement()
        {
            this.writer.WriteEndElement();
            this.depth--;
        }

        /// <summary>
        /// Writes out a start tag with the specified local name.
        /// </summary>
        /// <param name="name">The local name of the element.</param>
        internal void WriteStartElement(string name)
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
        protected override void CommitBuffer(int bytes)
        {
            this.WriteByteBuffer(bytes);
        }

        /// <inheritdoc />
        protected override Span<byte> RentBuffer(int maximumSize)
        {
            return new Span<byte>(this.byteBuffer);
        }

        private static XmlWriterSettings CreateXmlWriterSettings()
        {
            return new XmlWriterSettings
            {
                CheckCharacters = false,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                NewLineHandling = NewLineHandling.None,
                OmitXmlDeclaration = true,
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
