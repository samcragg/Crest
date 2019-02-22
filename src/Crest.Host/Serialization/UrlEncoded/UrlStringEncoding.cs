// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.UrlEncoded
{
    using System;
    using System.Runtime.CompilerServices;
    using Crest.Host.Conversion;
    using Crest.Host.Serialization.Json;

    /// <summary>
    /// Provides utility methods for writing URL escaped strings.
    /// </summary>
    internal static class UrlStringEncoding
    {
        /// <summary>
        /// Represents the maximum number of bytes a single character will be
        /// encoded to.
        /// </summary>
        public const int MaxBytesPerCharacter = 12; // %AA%BB%CC%DD

        /// <summary>
        /// Determines how to escape spaces.
        /// </summary>
        public enum SpaceOption
        {
            /// <summary>
            /// Percent encode spaces.
            /// </summary>
            Percent,

            /// <summary>
            /// Encode spaces as a single plus character.
            /// </summary>
            Plus,
        }

        /// <summary>
        /// Appends the specified value to the buffer.
        /// </summary>
        /// <param name="space">How to escape spaces.</param>
        /// <param name="ch">The character to append.</param>
        /// <param name="buffer">The buffer to output to.</param>
        /// <returns>The number of bytes written.</returns>
        public static int AppendChar(SpaceOption space, char ch, Span<byte> buffer)
        {
            if (TryAppendChar(space, buffer, ch))
            {
                return 1;
            }
            else
            {
                return AppendUtf32(buffer, ch);
            }
        }

        /// <summary>
        /// Appends the specified value to the buffer.
        /// </summary>
        /// <param name="space">How to escape spaces.</param>
        /// <param name="str">Contains the character to append.</param>
        /// <param name="index">
        /// The index of the character in the string. This will be updated to
        /// point past the end of the current code point.
        /// </param>
        /// <param name="buffer">The buffer to output to.</param>
        /// <returns>The number of bytes written.</returns>
        internal static int AppendChar(SpaceOption space, string str, ref int index, Span<byte> buffer)
        {
            int ch = str[index];
            if (TryAppendChar(space, buffer, ch))
            {
                return 1;
            }

            // Check if we're a surrogate pair
            if (ch >= 0xd800)
            {
                index++;
                if (index < str.Length)
                {
                    ch = char.ConvertToUtf32((char)ch, str[index]);
                }
            }

            return AppendUtf32(buffer, ch);
        }

        private static int AppendUtf32(Span<byte> output, int utf32)
        {
            Span<byte> utf8Buffer = stackalloc byte[4];
            int bytes = JsonStringEncoding.AppendUtf32(utf32, utf8Buffer);
            int index = 0;
            for (int i = 0; i < bytes; i++)
            {
                byte b = utf8Buffer[i];
                output[index] = 0x25; // '%'
                output[index + 1] = (byte)PrimitiveDigits.UppercaseHex[b >> 4];
                output[index + 2] = (byte)PrimitiveDigits.UppercaseHex[b & 0xf];
                index += 3;
            }

            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool InRange(int ch, uint lower, uint upper)
        {
            return ((uint)ch - lower) <= (upper - lower);
        }

        private static bool TryAppendChar(SpaceOption space, Span<byte> buffer, int ch)
        {
            // http://www.w3.org/TR/html5/forms.html#url-encoded-form-data
            // Happy path for ASCII letters/numbers
            if (InRange(ch, 0x30, 0x39) ||
                InRange(ch, 0x41, 0x5a) ||
                InRange(ch, 0x61, 0x7a))
            {
                buffer[0] = (byte)ch;
                return true;
            }
            else
            {
                // RFC 3986 § 2.3 Unreserved Characters
                switch (ch)
                {
                    case 0x20:
                        // Replace spaces with '+' - note if we should be percent
                        // encoding them then we return false and the '+' will
                        // be overwritten
                        buffer[0] = 0x2b;
                        return space == SpaceOption.Plus;

                    case 0x2d: // '-'
                    case 0x2e: // '.'
                    case 0x5f: // '_'
                    case 0x7e: // '~'
                        buffer[0] = (byte)ch; // Doesn't need escaping
                        return true;

                    default:
                        return false;
                }
            }
        }
    }
}
