// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Contains helper methods used during parsing of numbers.
    /// </summary>
    internal static class NumberParsing
    {
        // We use 18 as that's the maximum amount of significant digits for a
        // double - if it's greater we'll fall back to Math.Pow
        private static readonly double[] PowersOf10 =
        {
            1,
            1e+1,
            1e+2,
            1e+3,
            1e+4,
            1e+5,
            1e+6,
            1e+7,
            1e+8,
            1e+9,
            1e+10,
            1e+11,
            1e+12,
            1e+13,
            1e+14,
            1e+15,
            1e+16,
            1e+17,
            1e+18,
        };

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

        /// <summary>
        /// Parses an integer.
        /// </summary>
        /// <param name="span">Contains the characters to parse.</param>
        /// <param name="index">The index within the span to start parsing.</param>
        /// <param name="maximumDigits">The maximum number of digits to parse.</param>
        /// <param name="value">The parsed value.</param>
        /// <returns>The number of parsed digits.</returns>
        internal static int ParseDigits(ReadOnlySpan<char> span, ref int index, int maximumDigits, out uint value)
        {
            value = 0;
            int digits = 0;
            int originalIndex = index;
            for (; index < span.Length; index++)
            {
                uint digit = (uint)(span[index] - '0');
                if (digit > 9)
                {
                    break;
                }

                // Ignore leading zeros (in this case, digits would never be
                // incremented so adding zero to it still equals zero)
                if ((digits + digit) == 0)
                {
                    continue;
                }

                digits++;
                if (digits <= maximumDigits)
                {
                    value = (value * 10) + digit;
                }
            }

            return index - originalIndex;
        }

        /// <summary>
        /// Parses the exponent value in a string representing a floating-point
        /// value (e.g. e-123).
        /// </summary>
        /// <param name="span">Contains the characters to parse.</param>
        /// <param name="index">The index within the span to start parsing.</param>
        /// <returns>The value of the exponent to raise the value by.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ParseExponent(ReadOnlySpan<char> span, ref int index)
        {
            // Maximum exponent is 308, however, add a digit so we can detect
            // overflow (i.e. 1234 is too big but if we stopped at three we'd
            // get 123 which is OK)
            const int MaximumExponentDigits = 4;

            int originalIndex = index;
            if (index < span.Length)
            {
                char c = span[index];
                if ((c == 'e') || (c == 'E'))
                {
                    index++;
                    int sign = ParseSign(span, ref index);
                    if (ParseDigits(span, ref index, MaximumExponentDigits, out uint exponent) > 0)
                    {
                        return (int)exponent * sign;
                    }
                }
            }

            // We didn't parse a valid exponent, go back to the start
            index = originalIndex;
            return 0;
        }

        /// <summary>
        /// Parses (and consumes) the leading sign, if any.
        /// </summary>
        /// <param name="span">Contains the characters to parse.</param>
        /// <param name="index">The index within the span to start parsing.</param>
        /// <returns>
        /// A number that indicates the sign of value: -1 if the value should
        /// be less than zero, otherwise, 1.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int ParseSign(ReadOnlySpan<char> span, ref int index)
        {
            if (index < span.Length)
            {
                char c = span[index];
                if (c == '-')
                {
                    index++;
                    return -1;
                }
                else if (c == '+')
                {
                    index++;
                }
            }

            return 1;
        }

        /// <summary>
        /// Raises the number 10 to the specified power.
        /// </summary>
        /// <param name="power">Specifies the power.</param>
        /// <returns>The number 10 raised to the specified power.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Pow10(int power)
        {
            if (power <= 18)
            {
                return PowersOf10[power];
            }
            else
            {
                return Math.Pow(10, power);
            }
        }
    }
}
