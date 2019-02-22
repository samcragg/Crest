// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Json
{
    using System;
    using System.Runtime.CompilerServices;
    using Crest.Host.Conversion;
    using Crest.Host.IO;

    /// <summary>
    /// Provides utility methods for writing JSON escaped strings.
    /// </summary>
    internal static class JsonStringEncoding
    {
        /// <summary>
        /// Represents the maximum number of bytes a single character will be
        /// encoded to.
        /// </summary>
        public const int MaxBytesPerCharacter = 6; // The longest is \u00xx

        /// <summary>
        /// Appends the specified value to the buffer.
        /// </summary>
        /// <param name="ch">The character to append.</param>
        /// <param name="buffer">The buffer to output to.</param>
        /// <returns>The number of bytes written.</returns>
        public static int AppendChar(char ch, Span<byte> buffer)
        {
            if (NeedToEscape(ch))
            {
                return EscapeChar(ch, buffer);
            }
            else
            {
                return AppendUtf32(ch, buffer);
            }
        }

        /// <summary>
        /// Appends the specified value to the buffer.
        /// </summary>
        /// <param name="str">Contains the character to append.</param>
        /// <param name="index">
        /// The index of the character in the string. This will be updated to
        /// point past the end of the current code point.
        /// </param>
        /// <param name="buffer">The buffer to output to.</param>
        /// <returns>The number of bytes written.</returns>
        public static int AppendChar(string str, ref int index, Span<byte> buffer)
        {
            int ch = str[index];
            if (NeedToEscape(ch))
            {
                return EscapeChar(ch, buffer);
            }
            else
            {
                // We're converting UTF-16 to UTF-8, so we need to check if
                // we're a surrogate pair and, if so, encode a single UTF-32
                // code point as a UTF-8 sequence
                if (ch >= 0xd800)
                {
                    index++;
                    if (index < str.Length)
                    {
                        ch = char.ConvertToUtf32((char)ch, str[index]);
                    }
                }

                return AppendUtf32(ch, buffer);
            }
        }

        /// <summary>
        /// Decodes a character from the sequence, starting at the current
        /// position.
        /// </summary>
        /// <param name="source">The sequence of UTF-16 characters.</param>
        /// <returns>
        /// The character read from the stream, unescaped as required.
        /// </returns>
        /// <remarks>
        /// This method does not advance the stream if there is nothing to
        /// unescape (i.e. will not initially call
        /// <see cref="StreamIterator.MoveNext"/> on <c>source</c> if it
        /// contains an unescaped character).
        /// </remarks>
        public static char DecodeChar(ICharIterator source)
        {
            char c = source.Current;
            if (c == '\\')
            {
                return Unescape(source);
            }
            else
            {
                return c;
            }
        }

        /// <summary>
        /// Appends the specified UTF-32 code point as UTF-8 bytes.
        /// </summary>
        /// <param name="utf32">The code point to encode.</param>
        /// <param name="buffer">The buffer to output the bytes to.</param>
        /// <returns>The number of bytes written.</returns>
        internal static int AppendUtf32(int utf32, Span<byte> buffer)
        {
            if (utf32 < 0x80)
            {
                buffer[0] = (byte)utf32;
                return 1;
            }
            else if (utf32 < 0x800)
            {
                buffer[0] = (byte)(0xc0 | (utf32 >> 6));
                buffer[1] = (byte)(0x80 | (utf32 & 0x3f));
                return 2;
            }
            else if (utf32 < 0x010000)
            {
                buffer[0] = (byte)(0xe0 | (utf32 >> 12));
                buffer[1] = (byte)(0x80 | ((utf32 >> 6) & 0x3f));
                buffer[2] = (byte)(0x80 | (utf32 & 0x3f));
                return 3;
            }
            else
            {
                buffer[0] = (byte)(0xf0 | (utf32 >> 18));
                buffer[1] = (byte)(0x80 | ((utf32 >> 12) & 0x3f));
                buffer[2] = (byte)(0x80 | ((utf32 >> 6) & 0x3f));
                buffer[3] = (byte)(0x80 | (utf32 & 0x3f));
                return 4;
            }
        }

        private static Exception CreateUnexpectedEndOfStream()
        {
            return new FormatException("Unexpected end of stream.");
        }

        private static int EscapeChar(int ch, Span<byte> buffer)
        {
            byte b;
            switch (ch)
            {
                case '"':
                    b = (byte)'"';
                    break;

                case '\\':
                    b = (byte)'\\';
                    break;

                case '\b':
                    b = (byte)'b';
                    break;

                case '\f':
                    b = (byte)'f';
                    break;

                case '\n':
                    b = (byte)'n';
                    break;

                case '\r':
                    b = (byte)'r';
                    break;

                case '\t':
                    b = (byte)'t';
                    break;

                default:
                    buffer[0] = (byte)'\\';
                    buffer[1] = (byte)'u';
                    buffer[2] = (byte)'0';
                    buffer[3] = (byte)'0';
                    buffer[4] = (byte)PrimitiveDigits.LowercaseHex[ch >> 4];
                    buffer[5] = (byte)PrimitiveDigits.LowercaseHex[ch & 0xF];
                    return 6;
            }

            buffer[0] = (byte)'\\';
            buffer[1] = b;
            return 2;
        }

        private static int GetHexChar(ICharIterator source)
        {
            if (!source.MoveNext())
            {
                throw CreateUnexpectedEndOfStream();
            }

            uint c = source.Current;
            uint digit = c - '0';
            if (digit <= ('9' - '0'))
            {
                return (int)digit;
            }

            digit = c - 'a';
            if (digit <= ('f' - 'a'))
            {
                return (int)(digit + 10);
            }

            digit = c - 'A';
            if (digit <= ('F' - 'A'))
            {
                return (int)(digit + 10);
            }

            throw new FormatException(
                $"Invalid hexadecimal character '{source.Current}' at position {source.Position}.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NeedToEscape(int ch)
        {
            // Is it a control character or one that we always need to escape?
            return (ch < 32) || (ch == '"') || (ch == '\\');
        }

        private static char Unescape(ICharIterator source)
        {
            if (!source.MoveNext())
            {
                throw CreateUnexpectedEndOfStream();
            }

            switch (source.Current)
            {
                case '"':
                    return '"';

                case '\\':
                    return '\\';

                case '/':
                    return '/';

                case 'b':
                    return '\b';

                case 'f':
                    return '\f';

                case 'n':
                    return '\n';

                case 'r':
                    return '\r';

                case 't':
                    return '\t';

                case 'u':
                    return UnescapeHexSequence(source);

                default:
                    // -2 from the position to point to the start of the
                    // sequence (we've read \? so far)
                    throw new FormatException(
                        $"Unrecognized escape character '{source.Current}' at position {source.Position - 2}.");
            }
        }

        private static char UnescapeHexSequence(ICharIterator source)
        {
            int h1 = GetHexChar(source) << 12;
            int h2 = GetHexChar(source) << 8;
            int h3 = GetHexChar(source) << 4;
            int h4 = GetHexChar(source);

            return (char)(h1 | h2 | h3 | h4);
        }
    }
}
