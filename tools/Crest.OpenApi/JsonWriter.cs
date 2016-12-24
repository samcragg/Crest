// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System.IO;

    /// <summary>
    /// Provides helper methods for writing JSON to a text writer.
    /// </summary>
    internal abstract class JsonWriter
    {
        private readonly TextWriter writer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonWriter"/> class.
        /// </summary>
        /// <param name="writer">Where to write the output to.</param>
        protected JsonWriter(TextWriter writer)
        {
            this.writer = writer;
        }

        /// <summary>
        /// Writes a character to the output.
        /// </summary>
        /// <param name="value">The character to write to the text stream.</param>
        protected void Write(char value)
        {
            this.writer.Write(value);
        }

        /// <summary>
        /// Writes an escaped JSON string.
        /// </summary>
        /// <param name="value">The string to write to the text output.</param>
        protected void Write(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            foreach (char c in value)
            {
                if (char.IsControl(c) || (c == '"') || (c == '\\'))
                {
                    this.WriteEscapedChar(c);
                }
                else
                {
                    this.writer.Write(c);
                }
            }
        }

        /// <summary>
        /// Writes a string to the output without escaping any characters.
        /// </summary>
        /// <param name="value">The string to write to the text output.</param>
        protected void WriteRaw(string value)
        {
            this.writer.Write(value);
        }

        private void WriteEscapedChar(char c)
        {
            this.writer.Write('\\');
            switch (c)
            {
                case '\\':
                case '"':
                    this.writer.Write(c);
                    break;

                case '\b':
                    this.writer.Write('b');
                    break;

                case '\f':
                    this.writer.Write('f');
                    break;

                case '\n':
                    this.writer.Write('n');
                    break;

                case '\r':
                    this.writer.Write('r');
                    break;

                case '\t':
                    this.writer.Write('t');
                    break;

                default:
                    this.writer.Write("u{0:X4}", (int)c);
                    break;
            }
        }
    }
}
