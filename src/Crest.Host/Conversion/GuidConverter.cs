// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;

    /// <summary>
    /// Used to convert a <see cref="Guid"/> to/from a series of ASCII bytes.
    /// </summary>
    internal static partial class GuidConverter
    {
        /// <summary>
        /// Represents the maximum number of characters a GUID needs to be
        /// converted as text.
        /// </summary>
        public const int MaximumTextLength = 36;

        private const string InvalidHexadecimalCharacter = "Invalid hexadecimal character";
        private const string InvalidLength = "Invalid GUID length";
        private const string MissingHyphen = "Hyphens are in the incorrect location";

        /// <summary>
        /// Reads a GUID from the buffer.
        /// </summary>
        /// <param name="span">Contains the characters to parse.</param>
        /// <returns>The result of the parsing operation.</returns>
        public static ParseResult<Guid> TryReadGuid(ReadOnlySpan<char> span)
        {
            int index = 0;
            if (!CheckStringLength(span, ref index))
            {
                return new ParseResult<Guid>(InvalidLength);
            }

            int charactersSkippedAtStart = index;
            string error = null;
            Guid guid = ParseGuid(span, ref index, ref error);
            if (error != null)
            {
                return new ParseResult<Guid>(error);
            }

            // Add the number of characters we skipped at the start to allow
            // for matching them at the end
            return new ParseResult<Guid>(guid, index + charactersSkippedAtStart);
        }

        /// <summary>
        /// Converts a GUID value to human readable text.
        /// </summary>
        /// <param name="buffer">The byte array to output to.</param>
        /// <param name="offset">The index of where to start writing from.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The number of bytes written.</returns>
        public static int WriteGuid(byte[] buffer, int offset, Guid value)
        {
            // Quicker than ToByteArray + saves memory allocation
            var bytes = default(GuidBytes);
            bytes.Guid = value;

            // The format will be: "12345678-0123-5678-0123-567890123456"
            buffer[offset + 8] = (byte)'-';
            buffer[offset + 13] = (byte)'-';
            buffer[offset + 18] = (byte)'-';
            buffer[offset + 23] = (byte)'-';

            // NOTE: The GUID stores the first few bytes as integers/shorts in
            // the following format:
            //     private int _a;
            //     private short _b;
            //     private short _c;
            // Therefore, switch the first few bytes around
            // int _a
            WriteHexPair(buffer, offset + 0, bytes.B3, bytes.B2);
            WriteHexPair(buffer, offset + 4, bytes.B1, bytes.B0);

            // short _b
            WriteHexPair(buffer, offset + 9, bytes.B5, bytes.B4);

            // short _c
            WriteHexPair(buffer, offset + 14, bytes.B7, bytes.B6);

            // Back to bytes (e.g. byte _d)
            WriteHexPair(buffer, offset + 19, bytes.B8, bytes.B9);
            WriteHexPair(buffer, offset + 24, bytes.B10, bytes.B11);
            WriteHexPair(buffer, offset + 28, bytes.B12, bytes.B13);
            WriteHexPair(buffer, offset + 32, bytes.B14, bytes.B15);

            return MaximumTextLength;
        }

        private static bool CheckHyphens(in ReadOnlySpan<char> span, int start, ref string error)
        {
            // We've already checked the string is at least 32 characters
            // in CheckStringLength, so no need to do further bounds checking
            if (span[start + 8] == '-')
            {
                // 01234567-9012-4567-9012-456789012345
                if ((span[start + 13] != '-') ||
                    (span[start + 18] != '-') ||
                    (span[start + 23] != '-'))
                {
                    error = MissingHyphen;
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool CheckStringLength(in ReadOnlySpan<char> span, ref int start)
        {
            if (span.Length < 32)
            {
                return false;
            }

            if (span.Length >= 38)
            {
                char first = span[0];
                if (first == '{')
                {
                    start++;
                    return span[37] == '}';
                }
                else if (first == '(')
                {
                    start++;
                    return span[37] == ')';
                }
            }

            return true;
        }

        private static bool GetHexInt32(in ReadOnlySpan<char> span, int index, int end, out uint result)
        {
            result = 0;
            if (end > span.Length)
            {
                return false;
            }

            for (int i = index; i < end; i++)
            {
                char c = span[i];
                uint digit = (uint)(c - '0');
                if (digit > 10)
                {
                    digit = (uint)(c - 'a');
                    if (digit > 6)
                    {
                        digit = (uint)(c - 'A');
                        if (digit > 6)
                        {
                            return false;
                        }
                    }

                    result = (result * 16u) + 10u + digit;
                }
                else
                {
                    result = (result * 16u) + digit;
                }
            }

            return true;
        }

        private static Guid ParseGuid(in ReadOnlySpan<char> span, ref int index, ref string error)
        {
            void AdvanceIndex(bool skipHyphen, ref int i, int amount)
            {
                if (skipHyphen)
                {
                    i += amount + 1;
                }
                else
                {
                    i += amount;
                }
            }

            bool hasHyphens = CheckHyphens(span, index, ref error);
            if (error != null)
            {
                return default;
            }

            if (!GetHexInt32(span, index, index + 8, out uint a))
            {
                goto InvalidHex;
            }

            AdvanceIndex(hasHyphens, ref index, 8);
            if (!GetHexInt32(span, index, index + 4, out uint b))
            {
                goto InvalidHex;
            }

            AdvanceIndex(hasHyphens, ref index, 4);
            if (!GetHexInt32(span, index, index + 4, out uint c))
            {
                goto InvalidHex;
            }

            AdvanceIndex(hasHyphens, ref index, 4);
            if (!GetHexInt32(span, index, index + 4, out uint d))
            {
                goto InvalidHex;
            }

            AdvanceIndex(hasHyphens, ref index, 4);
            if (!GetHexInt32(span, index, index + 4, out uint e) ||
                !GetHexInt32(span, index + 4, index + 12, out uint f))
            {
                goto InvalidHex;
            }

            index += 12;
            return new Guid(
                (int)a,
                (short)b,
                (short)c,
                (byte)(d >> 8),
                (byte)d,
                (byte)(e >> 8),
                (byte)e,
                (byte)(f >> 24),
                (byte)(f >> 16),
                (byte)(f >> 8),
                (byte)f);

            InvalidHex:
            error = InvalidHexadecimalCharacter;
            return default;
        }

        private static void WriteHexPair(byte[] buffer, int offset, byte a, byte b)
        {
            buffer[offset] = (byte)PrimitiveDigits.LowercaseHex[a >> 4];
            buffer[offset + 1] = (byte)PrimitiveDigits.LowercaseHex[a & 0xF];
            buffer[offset + 2] = (byte)PrimitiveDigits.LowercaseHex[b >> 4];
            buffer[offset + 3] = (byte)PrimitiveDigits.LowercaseHex[b & 0xF];
        }
    }
}
