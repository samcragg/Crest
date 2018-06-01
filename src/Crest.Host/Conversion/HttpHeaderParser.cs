// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;
    using System.Collections.Generic;
    using Crest.Host.IO;
    using Crest.Host.Serialization;

    /// <summary>
    /// Parses headers according to
    /// <a href="https://tools.ietf.org/html/rfc7230">RFC 7230</a>.
    /// </summary>
    internal sealed partial class HttpHeaderParser
    {
        private readonly StringBuffer buffer = new StringBuffer();
        private readonly ICharIterator iterator;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpHeaderParser"/> class.
        /// </summary>
        /// <param name="iterator">Used to get the data to parse.</param>
        public HttpHeaderParser(ICharIterator iterator)
        {
            this.iterator = iterator;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpHeaderParser"/> class.
        /// </summary>
        /// <param name="value">Contains the data to parse.</param>
        internal HttpHeaderParser(string value)
            : this(new StringIterator(value))
        {
            // Prime the iterator for reading tokens (we can't do this in the
            // normal constructor as ReadPairs needs to be able to handle
            // empty streams so calls MoveNext before looping)
            this.iterator.MoveNext();
        }

        /// <summary>
        /// Reads the header fields.
        /// </summary>
        /// <returns>A dictionary containing the name/value pairs.</returns>
        public IReadOnlyDictionary<string, string> ReadPairs()
        {
            // As we're only expected a few header values (under eight),
            // benchmarking shows the SortedDictionary uses less memory and
            // is quicker to allocate (we're optimizing for the case of create
            // and read a value from it, not repeated reading)
            var dictionary = new SortedDictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            // HTTP-message = start-line
            //                *(header-field CRLF)
            //                CRLF
            //
            // Since we're inside a part of the multipart body, we won't have
            // the start-line to parse and the trailing empty line (CRLF) has
            // already been found and removed for us
            while (this.iterator.MoveNext())
            {
                this.AddPair(dictionary);
            }

            return dictionary;
        }

        /// <summary>
        /// Reads the character at the current position and skips if it matches
        /// the expected value.
        /// </summary>
        /// <param name="expected">The character to read.</param>
        /// <returns>
        /// <c>true</c> if the expected character was read; otherwise, <c>false</c>.
        /// </returns>
        internal bool ReadCharacter(char expected)
        {
            if (this.iterator.Current != expected)
            {
                return false;
            }
            else
            {
                this.iterator.MoveNext();
                return true;
            }
        }

        /// <summary>
        /// Reads a attribute/value pair from the current position.
        /// </summary>
        /// <param name="attribute">
        /// When this method returns, contains the parsed attribute name.
        /// </param>
        /// <param name="value">
        /// When this method returns, contains the parsed attribute value.
        /// </param>
        /// <returns>
        /// <c>true</c> if a parameter was read; otherwise, <c>false</c>.
        /// </returns>
        internal bool ReadParameter(out string attribute, out string value)
        {
            // parameter := attribute "=" value
            //
            // attribute := token
            //              ; Matching of attributes is ALWAYS case-insensitive.
            //
            // value := token / quoted-string
            this.SkipWhiteSpace();
            if (!this.ReadToken(out attribute) || !this.ReadCharacter('='))
            {
                attribute = null;
                value = null;
                return false;
            }

            if (this.ReadQuotedString(out value))
            {
                return true;
            }

            return this.ReadToken(out value);
        }

        /// <summary>
        /// Reads a token from the current position.
        /// </summary>
        /// <param name="token">
        /// When this method returns, contains the parsed token.
        /// </param>
        /// <returns>
        /// <c>true</c> if a value token was read; otherwise, <c>false</c>.
        /// </returns>
        internal bool ReadToken(out string token)
        {
            // token = 1*tchar
            this.buffer.Clear();
            do
            {
                if (!IsTokenChar(this.iterator.Current))
                {
                    break;
                }

                this.buffer.Append(this.iterator.Current);
            }
            while (this.iterator.MoveNext());

            // We need at least one character for a token
            token = this.buffer.ToString();
            return token.Length > 0;
        }

        private static bool IsOpaqueData(char current)
        {
            // obs-text = %x80-FF
            return current >= 0x80;
        }

        private static bool IsQuotedTextChar(char value)
        {
            // qdtext = HTAB / SP / %x21 / %x23-5B / %x5D-7E / obs-text
            if (((value - 0x23u) <= (0x5Bu - 0x23u)) ||
                ((value - 0x5Du) <= (0x7Eu - 0x5Du)))
            {
                return true;
            }
            else
            {
                return
                    (value == Tokens.HTab) ||
                    (value == Tokens.Space) ||
                    (value == 0x21) ||
                    IsOpaqueData(value);
            }
        }

        private static bool IsTokenChar(char value)
        {
            // tchar = "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-"
            //       / "." / "^" / "_" / "`" / "|" / "~" / DIGIT / ALPHA
            if (((value - (uint)'a') <= ((uint)'z' - 'a')) ||
                ((value - (uint)'A') <= ((uint)'Z' - 'A')) ||
                ((value - (uint)'0') <= ((uint)'9' - '0')))
            {
                return true;
            }
            else
            {
                switch (value)
                {
                    case '!':
                    case '#':
                    case '$':
                    case '%':
                    case '&':
                    case '\'':
                    case '*':
                    case '+':
                    case '-':
                    case '.':
                    case '^':
                    case '_':
                    case '`':
                    case '|':
                    case '~':
                        return true;

                    default:
                        return false;
                }
            }
        }

        private static bool IsVisibleCharacter(char value)
        {
            // RFC5234:
            // VCHAR =  %x21-7E
            return (value - 0x21u) <= (0x7Eu - 0x21u);
        }

        private void AddPair(IDictionary<string, string> target)
        {
            // header-field = field-name ":" OWS field-value OWS
            // field-name   = token
            if (!this.ReadToken(out string fieldName) ||
                !this.ReadCharacter(Tokens.Colon))
            {
                throw this.CreateUnexpectedCharacter();
            }

            this.SkipWhiteSpace();
            this.ReadFieldValue(out string fieldValue);
            this.SkipWhiteSpace();

            if (!this.ReadNewLine())
            {
                throw this.CreateUnexpectedCharacter();
            }

            // Overwrite existing values
            target[fieldName] = fieldValue;
        }

        private FormatException CreateUnexpectedCharacter()
        {
            char value = this.iterator.Current;
            if (value == default)
            {
                return new FormatException("Unexpected end of header");
            }
            else
            {
                string ch = value.ToString();
                return new FormatException(
                    "Unexpected character '" + ch + "' inside header");
            }
        }

        private void ReadFieldContent()
        {
            // field-content = field-vchar [ 1*( SP / HTAB ) field-vchar ]
            // field-vchar   = VCHAR / obs-text
            //
            // The above basically allows whitespace inside the token but not
            // at the ends, hence why we keep track of the number of whitespace
            // characters added in case we have to remove them at the end
            int trailingWhiteSpace = 0;
            do
            {
                char current = this.iterator.Current;
                if (IsVisibleCharacter(current) || IsOpaqueData(current))
                {
                    this.buffer.Append(current);
                    trailingWhiteSpace = 0;
                }
                else if ((current == Tokens.Space) || (current == Tokens.HTab))
                {
                    this.buffer.Append(current);
                    trailingWhiteSpace++;
                }
                else
                {
                    break;
                }
            }
            while (this.iterator.MoveNext());

            this.buffer.Truncate(trailingWhiteSpace);
        }

        private void ReadFieldValue(out string value)
        {
            if (!this.ReadQuotedString(out value))
            {
                this.buffer.Clear();
                this.ReadFieldContent();
                value = this.buffer.ToString();
            }
        }

        private bool ReadNewLine()
        {
            return (this.iterator.Current == Tokens.CR) &&
                   this.iterator.MoveNext() &&
                   (this.iterator.Current == Tokens.LF);
        }

        private bool ReadQuotedPair()
        {
            // quoted-pair   = "\" (HTAB / SP / VCHAR / obs-text)
            if ((this.iterator.Current == Tokens.Backslash) && this.iterator.MoveNext())
            {
                char current = this.iterator.Current;
                if (IsVisibleCharacter(current) ||
                    (current == Tokens.HTab) ||
                    (current == Tokens.Space) ||
                    IsOpaqueData(current))
                {
                    this.buffer.Append(current);
                    return true;
                }
            }

            return false;
        }

        private bool ReadQuotedString(out string value)
        {
            // quoted-string = DQUOTE *(qdtext / quoted-pair) DQUOTE
            if (this.iterator.Current != Tokens.DQuote)
            {
                value = null;
                return false;
            }

            this.buffer.Clear();

            // We want to skip the quote, so MoveNext at the start of the loop
            while (this.iterator.MoveNext())
            {
                if (this.iterator.Current == Tokens.DQuote)
                {
                    this.iterator.MoveNext();
                    value = this.buffer.ToString();
                    return true;
                }

                if (!this.ReadQuotedPair())
                {
                    if (!IsQuotedTextChar(this.iterator.Current))
                    {
                        throw this.CreateUnexpectedCharacter();
                    }

                    this.buffer.Append(this.iterator.Current);
                }
            }

            // If we got here we didn't find the end quote
            throw this.CreateUnexpectedCharacter();
        }

        private void SkipWhiteSpace()
        {
            do
            {
                char current = this.iterator.Current;
                if ((current != Tokens.Space) && (current != Tokens.HTab))
                {
                    break;
                }
            }
            while (this.iterator.MoveNext());
        }
    }
}
