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
        /// <param name="value">The value to convert.</param>
        /// <returns>The number of bytes written.</returns>
        public static int WriteGuid(Span<byte> buffer, Guid value)
        {
            // Quicker than ToByteArray + saves memory allocation
            var bytes = default(GuidBytes);
            bytes.Guid = value;

            // The format will be: "12345678-0123-5678-0123-567890123456"
            buffer[8] = (byte)'-';
            buffer[13] = (byte)'-';
            buffer[18] = (byte)'-';
            buffer[23] = (byte)'-';

            // NOTE: The GUID stores the first few bytes as integers/shorts in
            // the following format:
            //     int _a
            //     short _b
            //     short _c
            // Therefore, switch the first few bytes around
            // int _a
            WriteHexPair(buffer, 0, bytes.B3, bytes.B2);
            WriteHexPair(buffer, 4, bytes.B1, bytes.B0);

            // short _b
            WriteHexPair(buffer, 9, bytes.B5, bytes.B4);

            // short _c
            WriteHexPair(buffer, 14, bytes.B7, bytes.B6);

            // Back to bytes (e.g. byte _d)
            WriteHexPair(buffer, 19, bytes.B8, bytes.B9);
            WriteHexPair(buffer, 24, bytes.B10, bytes.B11);
            WriteHexPair(buffer, 28, bytes.B12, bytes.B13);
            WriteHexPair(buffer, 32, bytes.B14, bytes.B15);

            return MaximumTextLength;
        }

        private static bool CheckHyphens(ReadOnlySpan<char> span, int start, ref string error)
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

        private static bool CheckStringLength(ReadOnlySpan<char> span, ref int start)
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

        private static bool GetHexInt32(ReadOnlySpan<char> span, int index, int end, out uint result)
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
                if (digit > 9)
                {
                    digit = (uint)(c - 'a');
                    if (digit > 5)
                    {
                        digit = (uint)(c - 'A');
                        if (digit > 5)
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

        private static Guid InvalidHexadecimalCharacter(ref string error)
        {
            error = "Invalid hexadecimal character";
            return default;
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
                return InvalidHexadecimalCharacter(ref error);
            }

            AdvanceIndex(hasHyphens, ref index, 8);
            if (!GetHexInt32(span, index, index + 4, out uint b))
            {
                return InvalidHexadecimalCharacter(ref error);
            }

            AdvanceIndex(hasHyphens, ref index, 4);
            if (!GetHexInt32(span, index, index + 4, out uint c))
            {
                return InvalidHexadecimalCharacter(ref error);
            }

            AdvanceIndex(hasHyphens, ref index, 4);
            if (!GetHexInt32(span, index, index + 4, out uint d))
            {
                return InvalidHexadecimalCharacter(ref error);
            }

            AdvanceIndex(hasHyphens, ref index, 4);
            if (!GetHexInt32(span, index, index + 4, out uint e) ||
                !GetHexInt32(span, index + 4, index + 12, out uint f))
            {
                return InvalidHexadecimalCharacter(ref error);
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
        }

        private static void WriteHexPair(Span<byte> buffer, int offset, byte a, byte b)
        {
            buffer[offset] = (byte)PrimitiveDigits.LowercaseHex[a >> 4];
            buffer[offset + 1] = (byte)PrimitiveDigits.LowercaseHex[a & 0xF];
            buffer[offset + 2] = (byte)PrimitiveDigits.LowercaseHex[b >> 4];
            buffer[offset + 3] = (byte)PrimitiveDigits.LowercaseHex[b & 0xF];
        }
    }
}
