// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Xml;
    using Crest.Host.Serialization.Internal;
    using static System.Diagnostics.Debug;

    /// <summary>
    /// Allows the reading of values from a stream representing XML data.
    /// </summary>
    internal sealed class XmlStreamReader : ValueReader, IDisposable
    {
        private static readonly XmlReaderSettings DefaultWriterSettings = CreateXmlReaderSettings();
        private static readonly char[] FalseString = "false".ToCharArray();
        private static readonly char[] TrueString = "true".ToCharArray();

        private readonly XmlReader reader;
        private readonly StringBuffer stringBuffer = new StringBuffer();
        private bool currentElementIsEmpty;
        private bool currentElementIsNil;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlStreamReader"/> class.
        /// </summary>
        /// <param name="stream">The stream to read the values from.</param>
        public XmlStreamReader(Stream stream)
        {
            this.reader = XmlReader.Create(stream, DefaultWriterSettings);
        }

        /// <summary>
        /// Gets the nesting level of the element in the XML.
        /// </summary>
        internal int Depth => this.reader.Depth;

        /// <summary>
        /// Releases the resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            this.reader.Dispose();
            this.stringBuffer.Dispose();
        }

        /// <inheritdoc />
        public override bool ReadBoolean()
        {
            // http://www.w3.org/TR/xmlschema-2/#boolean
            // Valid values are: true, false, 1, 0
            ReadOnlySpan<char> content = this.ReadTrimmedString();
            if (content.SequenceEqual(TrueString) || AreEqual(content, '1'))
            {
                return true;
            }
            else if (content.SequenceEqual(FalseString) || AreEqual(content, '0'))
            {
                return false;
            }
            else
            {
                throw new FormatException($"Invalid boolean value at {this.GetCurrentPosition()}");
            }
        }

        /// <inheritdoc />
        public override char ReadChar()
        {
            string value = null;
            if (!this.currentElementIsEmpty)
            {
                IEnumerator<string> iterator = this.IterateElementContents().GetEnumerator();
                while (iterator.MoveNext())
                {
                    value = iterator.Current;
                    if (!string.IsNullOrEmpty(value))
                    {
                        break;
                    }
                }
            }

            if ((value == null) || (value.Length != 1))
            {
                throw new FormatException("Expected a single character at " + this.GetCurrentPosition());
            }

            return value[0];
        }

        /// <inheritdoc />
        public override bool ReadNull()
        {
            return this.currentElementIsNil;
        }

        /// <inheritdoc />
        public override string ReadString()
        {
            if (this.currentElementIsEmpty)
            {
                return string.Empty;
            }
            else
            {
                IEnumerator<string> iterator = this.IterateElementContents().GetEnumerator();
                iterator.MoveNext();
                string first = iterator.Current;
                if (iterator.MoveNext())
                {
                    // Use the buffer to join them
                    this.stringBuffer.Clear();
                    this.stringBuffer.Append(first);
                    do
                    {
                        this.stringBuffer.Append(iterator.Current);
                    }
                    while (iterator.MoveNext());

                    return this.stringBuffer.ToString();
                }
                else
                {
                    return first;
                }
            }
        }

        /// <summary>
        /// Returns a value indicating whether a start element is ready to be
        /// read or not.
        /// </summary>
        /// <returns>
        /// <c>true</c> if <see cref="ReadStartElement"/> can be called
        /// successfully; otherwise, <c>false</c>.
        /// </returns>
        internal bool CanReadStartElement()
        {
            return this.reader.MoveToContent() == XmlNodeType.Element;
        }

        /// <inheritdoc />
        internal override string GetCurrentPosition()
        {
            var lineInfo = (IXmlLineInfo)this.reader;
            return $"line: {lineInfo.LineNumber}, column: {lineInfo.LinePosition}";
        }

        /// <summary>
        /// Checks that the current content node is an end tag and advances the
        /// reader to the next node.
        /// </summary>
        internal void ReadEndElement()
        {
            if (!this.currentElementIsEmpty)
            {
                if (this.reader.MoveToContent() != XmlNodeType.EndElement)
                {
                    throw new FormatException("Expected to find an end element at " + this.GetCurrentPosition());
                }

                this.reader.Read();
            }

            this.currentElementIsEmpty = false;
            this.currentElementIsNil = false;
        }

        /// <summary>
        /// Checks that the current content node is a start tag and advances
        /// the reader to the next node.
        /// </summary>
        /// <returns>The name of the start element.</returns>
        internal string ReadStartElement()
        {
            if (this.reader.MoveToContent() != XmlNodeType.Element)
            {
                throw new FormatException("Expected to find a start element at " + this.GetCurrentPosition());
            }

            // Grab the information before we move past the start element
            this.currentElementIsEmpty = this.reader.IsEmptyElement;
            string elementName = this.reader.Name;
            this.currentElementIsNil = this.ReadNilAttribute();

            // Consume the start element before exiting
            this.reader.Read();
            return elementName;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AreEqual(ReadOnlySpan<char> span, char c)
        {
            return (span.Length == 1) && (span[0] == c);
        }

        private static XmlReaderSettings CreateXmlReaderSettings()
        {
            return new XmlReaderSettings
            {
                CheckCharacters = false,
                CloseInput = true,
                ConformanceLevel = ConformanceLevel.Fragment,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                IgnoreWhitespace = true,
            };
        }

        private static bool IsWhiteSpace(char c)
        {
            // http://www.w3.org/TR/REC-xml/#sec-common-syn
            // S ::= (#x20 | #x9 | #xD | #xA)+
            return (c == '\x20') || (c == '\x9') || (c == '\xD') || (c == '\xA');
        }

        /// <inheritdoc />
        private protected override ReadOnlySpan<char> ReadTrimmedString()
        {
            if (this.currentElementIsEmpty)
            {
                return ReadOnlySpan<char>.Empty;
            }
            else
            {
                this.stringBuffer.Clear();

                IEnumerator<string> iterator = this.IterateElementContents().GetEnumerator();
                while (iterator.MoveNext())
                {
                    this.stringBuffer.Append(iterator.Current);
                }

                this.stringBuffer.TrimEnds(IsWhiteSpace);
                return this.stringBuffer.CreateSpan();
            }
        }

        private IEnumerable<string> IterateElementContents()
        {
            Assert(!this.currentElementIsEmpty, "Cannot be called on empty elements");
            this.reader.MoveToContent();
            yield return this.reader.Value;

            bool keepReading = this.reader.Read();
            while (keepReading)
            {
                switch (this.reader.NodeType)
                {
                    case XmlNodeType.CDATA:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.Text:
                        yield return this.reader.Value;
                        keepReading = this.reader.Read();
                        break;

                    default:
                        keepReading = false;
                        break;
                }
            }
        }

        private bool ReadNilAttribute()
        {
            const string NilNamespace = "http://www.w3.org/2001/XMLSchema-instance";

            if (this.reader.MoveToFirstAttribute())
            {
                do
                {
                    if (string.Equals(this.reader.LocalName, "nil", StringComparison.Ordinal) &&
                        string.Equals(this.reader.NamespaceURI, NilNamespace, StringComparison.Ordinal))
                    {
                        return string.Equals(this.reader.Value, "true", StringComparison.Ordinal);
                    }
                }
                while (this.reader.MoveToNextAttribute());
            }

            return false;
        }
    }
}
