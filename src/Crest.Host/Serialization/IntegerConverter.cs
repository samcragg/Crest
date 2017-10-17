// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
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

        /// <summary>
        /// Converts a signed 64-bit integer to human readable text.
        /// </summary>
        /// <param name="buffer">The byte array to output to.</param>
        /// <param name="offset">The index of where to start writing from.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The number of bytes written.</returns>
        public static int WriteInt64(byte[] buffer, int offset, long value)
        {
            if (value < 0)
            {
                buffer[offset] = (byte)'-';
                return WriteUInt64(buffer, offset + 1, (ulong)-value) + 1;
            }
            else
            {
                return WriteUInt64(buffer, offset, (ulong)value);
            }
        }

        /// <summary>
        /// Converts an unsigned 64-bit integer to human readable text.
        /// </summary>
        /// <param name="buffer">The byte array to output to.</param>
        /// <param name="offset">The index of where to start writing from.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The number of bytes written.</returns>
        public static int WriteUInt64(byte[] buffer, int offset, ulong value)
        {
            if (value == 0)
            {
                buffer[offset] = (byte)'0';
                return 1;
            }
            else
            {
                int digitCount = CountDigits(value);
                int index = offset + digitCount;

                // 32 bit arithmetic seems faster than 64 bit, even on a 64-bit CPU!?
                while (value > uint.MaxValue)
                {
                    value = DivRem(value, 100, out int digits);
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
            int digits = 0;

            // 32 bit arithmetic is measurably faster than 64 bit (even running
            // on a 64-bit CPU!?)
            while (value > uint.MaxValue)
            {
                // uint.Max equals 4,294,967,295, hence divide by 1,000,000,000
                // to reduce the amount of divisions
                digits += 9;
                value /= 1000000000;
            }

            return digits + CountDigits32((uint)value);
        }

        /// <summary>
        /// Calculates the quotient of two 64-bit unsigned integers and also
        /// returns the remainder in an output parameter.
        /// </summary>
        /// <param name="dividend">The dividend.</param>
        /// <param name="divisor">The divisor.</param>
        /// <param name="remainder">
        /// When this method returns, contains the remainder.
        /// </param>
        /// <returns>The quotient of the specified numbers.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong DivRem(ulong dividend, ulong divisor, out int remainder)
        {
            // See https://github.com/dotnet/coreclr/issues/3439 for why we use
            // multiplication instead of the modulus operator (basically this
            // avoids two divide machine instructions)
            ulong quotient = dividend / divisor;
            remainder = (int)(dividend - (quotient * divisor));
            return quotient;
        }

        /// <summary>
        /// Calculates the quotient of two 32-bit unsigned integers and also
        /// returns the remainder in an output parameter.
        /// </summary>
        /// <param name="dividend">The dividend.</param>
        /// <param name="divisor">The divisor.</param>
        /// <param name="remainder">
        /// When this method returns, contains the remainder.
        /// </param>
        /// <returns>The quotient of the specified numbers.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint DivRem(uint dividend, uint divisor, out int remainder)
        {
            // See https://github.com/dotnet/coreclr/issues/3439 for why we use
            // multiplication instead of the modulus operator (basically this
            // avoids two divide machine instructions)
            uint quotient = dividend / divisor;
            remainder = (int)(dividend - (quotient * divisor));
            return quotient;
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

        private static void WriteUInt32(byte[] buffer, int index, uint value)
        {
            // Do all the digit pairs
            while (value > 9)
            {
                value = DivRem(value, 100, out int digits);
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
