// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Converts an integer to/from a series of ASCII bytes.
    /// </summary>
    internal static class IntegerConverter
    {
        /// <summary>
        /// Represents the maximum number of characters a number needs to be
        /// converted as text.
        /// </summary>
        public const int MaximumTextLength = 20; // long.MinValue = -9223372036854775808

        private const string DigitExpected = "Digit expected";
        private const string Overflow = "The value is outside the valid integer range";

        /// <summary>
        /// Reads a signed 64-bit integer from the buffer.
        /// </summary>
        /// <param name="span">Contains the characters to parse.</param>
        /// <param name="min">
        /// The minimum value that will fit in the target integer type.
        /// </param>
        /// <param name="max">
        /// The maximum value that will fit in the target integer type.
        /// </param>
        /// <returns>The result of the parsing operation.</returns>
        public static ParseResult<long> TryReadSignedInt(
            ReadOnlySpan<char> span,
            long min,
            long max)
        {
            string error = null;
            int index = 0;
            bool negative = IsNegative(span, ref index);
            ulong integer = TryReadUInt64(span, ref index, ref error);
            if (error != null)
            {
                return new ParseResult<long>(error);
            }

            if (negative)
            {
                if (integer > (ulong)(min * -1L))
                {
                    return new ParseResult<long>(Overflow);
                }

                return new ParseResult<long>((long)integer * -1L, index);
            }
            else
            {
                if (integer > (ulong)max)
                {
                    return new ParseResult<long>(Overflow);
                }

                return new ParseResult<long>((long)integer, index);
            }
        }

        /// <summary>
        /// Reads an unsigned 64-bit integer from the buffer.
        /// </summary>
        /// <param name="span">Contains the characters to parse.</param>
        /// <param name="max">
        /// The maximum value that will fit in the target integer type.
        /// </param>
        /// <returns>The result of the parsing operation.</returns>
        public static ParseResult<ulong> TryReadUnsignedInt(ReadOnlySpan<char> span, ulong max)
        {
            string error = null;
            int index = 0;
            ulong integer = TryReadUInt64(span, ref index, ref error);
            if (error != null)
            {
                return new ParseResult<ulong>(error);
            }

            if (integer > max)
            {
                return new ParseResult<ulong>(Overflow);
            }

            return new ParseResult<ulong>(integer, index);
        }

        /// <summary>
        /// Converts a signed 64-bit integer to human readable text.
        /// </summary>
        /// <param name="buffer">The byte array to output to.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The number of bytes written.</returns>
        public static int WriteInt64(in Span<byte> buffer, long value)
        {
            if (value < 0)
            {
                buffer[0] = (byte)'-';
                return WriteUInt64(buffer.Slice(1), (ulong)-value) + 1;
            }
            else
            {
                return WriteUInt64(buffer, (ulong)value);
            }
        }

        /// <summary>
        /// Converts an unsigned 64-bit integer to human readable text.
        /// </summary>
        /// <param name="buffer">The byte array to output to.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The number of bytes written.</returns>
        public static int WriteUInt64(in Span<byte> buffer, ulong value)
        {
            if (value == 0)
            {
                buffer[0] = (byte)'0';
                return 1;
            }
            else
            {
                int digitCount = CountDigits(value);
                int index = digitCount;

                // 32 bit arithmetic seems faster than 64 bit, even on a 64-bit CPU!?
                while (value > uint.MaxValue)
                {
                    value = NumberParsing.DivRem(value, 100, out int digits);
                    buffer[--index] = (byte)PrimitiveDigits.Units[digits];
                    buffer[--index] = (byte)PrimitiveDigits.Tens[digits];
                }

                WriteUInt32(buffer, index, (uint)value);
                return digitCount;
            }
        }

        /// <summary>
        /// Calculates the number of bytes required to represent the specified
        /// value as text.
        /// </summary>
        /// <param name="value">The value to count the digits of.</param>
        /// <returns>The number of ASCII digits to represent the value.</returns>
        internal static int CountDigits(ulong value)
        {
            if (value == 0)
            {
                return 1;
            }

            int digits = 0;

            // 32 bit arithmetic is measurably faster than 64 bit (even running
            // on a 64-bit CPU!?)
            while (value > uint.MaxValue)
            {
                // uint.Max equals 4,294,967,295, hence divide by 1,000,000,000
                // to reduce the amount of divisions
                digits += 9;
                value /= (ulong)1e9;
            }

            return digits + CountDigits32((uint)value);
        }

        /// <summary>
        /// Attempts to parse an unsigned 64-bit value.
        /// </summary>
        /// <param name="span">Contains the characters to parse.</param>
        /// <param name="index">The index within the span to start parsing.</param>
        /// <param name="error">Will contain any errors encountered.</param>
        /// <returns>The parsed value.</returns>
        internal static ulong TryReadUInt64(ReadOnlySpan<char> span, ref int index, ref string error)
        {
            // Start with a value over 9 in case we've got an empty span
            ulong value = ulong.MaxValue;
            if (index < span.Length)
            {
                value = (uint)(span[index++] - '0');
            }

            if (value > 9)
            {
                error = DigitExpected;
                return 0;
            }

            for (; index < span.Length; index++)
            {
                uint digit = (uint)(span[index] - '0');
                if (digit > 9)
                {
                    break;
                }

                // Check for overflow
                ulong newValue = (value * 10u) + digit;
                if (newValue < value)
                {
                    error = Overflow;
                    return ulong.MaxValue;
                }

                value = newValue;
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountDigits32(uint value)
        {
            int pairs = 0;
            while (value > 9)
            {
                pairs++;
                value /= 100;
            }

            return (pairs * 2) + ((value > 0) ? 1 : 0);
        }

        private static bool IsNegative(ReadOnlySpan<char> span, ref int index)
        {
            char c = default;
            if (index < span.Length)
            {
                c = span[index];
            }

            if (c == '-')
            {
                index++;
                return true;
            }
            else
            {
                if (c == '+')
                {
                    index++;
                }

                return false;
            }
        }

        private static void WriteUInt32(in Span<byte> buffer, int index, uint value)
        {
            // Do all the digit pairs
            while (value > 9)
            {
                value = NumberParsing.DivRem(value, 100, out int digits);
                buffer[--index] = (byte)PrimitiveDigits.Units[digits];
                buffer[--index] = (byte)PrimitiveDigits.Tens[digits];
            }

            // Any single digits we need to worry about?
            if (value > 0)
            {
                buffer[--index] = (byte)PrimitiveDigits.Units[(int)value];
            }
        }
    }
}
