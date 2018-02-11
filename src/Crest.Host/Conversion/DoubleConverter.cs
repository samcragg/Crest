// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;
    using System.Runtime.CompilerServices;
    using static System.Diagnostics.Debug;

    /// <summary>
    /// Converts double-precision floating-point number to/from a series of
    /// ASCII bytes.
    /// </summary>
    internal static partial class DoubleConverter
    {
        private const int Emin = 308;
        private const string InvalidFormat = "Invalid format";
        private const int MaxExponentDigits = 3;
        private const int MaxSignificandDigits = 18;

        /// <summary>
        /// Reads a double from the specified value.
        /// </summary>
        /// <param name="span">Contains the characters to parse.</param>
        /// <returns>The result of the parsing operation.</returns>
        public static ParseResult<double> TryReadDouble(ReadOnlySpan<char> span)
        {
            int index = 0;
            int sign = NumberParsing.ParseSign(span, ref index);

            NumberInfo number = default;
            if (ParseSignificand<DotSeparator>(span, ref index, ref number))
            {
                number.Scale += (short)NumberParsing.ParseExponent(span, ref index);
                double value = MakeDouble(number, sign);
                return new ParseResult<double>(value, index);
            }

            // Not a number, is it a constant?
            index = 0;
            if (TryParseNamedConstant(span, ref index, out double constant))
            {
                return new ParseResult<double>(constant, index);
            }
            else
            {
                return new ParseResult<double>(InvalidFormat);
            }
        }

        /// <summary>
        /// Parses a number in the format of decimal followed by an optional
        /// fraction part.
        /// </summary>
        /// <typeparam name="T">
        /// Used to check for the separator (see remarks)
        /// </typeparam>
        /// <param name="span">Contains the characters to parse.</param>
        /// <param name="index">The index within the span to start parsing.</param>
        /// <param name="number">Populated with the parsed information.</param>
        /// <returns>
        /// <c>true</c> if any information was parsed; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// We're abusing generics here to allow the decimal separator check to
        /// be inlined (passing in a delegate adds to memory pressure and also
        /// can't be inlined, yet the JIT is able to inline the call on the
        /// generic type)
        /// </remarks>
        internal static bool ParseSignificand<T>(ReadOnlySpan<char> span, ref int index, ref NumberInfo number)
            where T : struct, INumericTokens
        {
            int originalIndex = index;
            ParseDigits(span, ref index, ref number);
            int digits = index - originalIndex;

            // Is there a decimal part as well?
            T tokens = default;
            if ((index < span.Length) && tokens.IsDecimalSeparator(span[index]))
            {
                index++; // Skip the separator
                int integerDigits = number.Digits;
                ParseDigits(span, ref index, ref number);
                number.Scale += (short)(integerDigits - number.Digits);

                // Check it's not just a decimal point
                if ((index - originalIndex) == 1)
                {
                    index = originalIndex;
                }
            }

            return index != originalIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double MakeDouble(in NumberInfo number, int sign)
        {
            long significand = sign * (long)number.Significand;

            // Improve the accuracy of the result for negative exponents
            if (number.Scale < 0)
            {
                // Allow for denormalized numbers
                if (number.Scale < -Emin)
                {
                    return (significand / NumberParsing.Pow10(-Emin - number.Scale)) / 1e308;
                }
                else
                {
                    return significand / NumberParsing.Pow10(-number.Scale);
                }
            }
            else
            {
                return significand * NumberParsing.Pow10(number.Scale);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ParseDigits(ReadOnlySpan<char> span, ref int index, ref NumberInfo number)
        {
            for (; index < span.Length; index++)
            {
                uint digit = (uint)(span[index] - '0');
                if (digit > 9)
                {
                    break;
                }

                // Ignore leading zeros. In this case Digits will be zero and
                // digit will be zero - adding together gives zero
                if ((number.Digits + digit) == 0)
                {
                    continue;
                }

                number.Digits++;
                if (number.Digits >= MaxSignificandDigits)
                {
                    continue;
                }

                number.Significand = (number.Significand * 10) + digit;
            }
        }

        private static bool StartsWith(ReadOnlySpan<char> span, int index, string value)
        {
            Assert(value == value.ToUpperInvariant(), "Passed in constant must be uppercase");
            if (index + value.Length > span.Length)
            {
                return false;
            }

            const uint UpperToLowerDelta = 'a' - 'A';
            for (int i = 0; i < value.Length; i++)
            {
                uint delta = (uint)(span[index++] - value[i]);
                if ((delta != 0) && (delta != UpperToLowerDelta))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseNamedConstant(ReadOnlySpan<char> span, ref int index, out double value)
        {
            if (StartsWith(span, index, "NAN"))
            {
                value = double.NaN;
                index += 3;
                return true;
            }

            // Infinity can be positive or negative
            int sign = NumberParsing.ParseSign(span, ref index);
            if (StartsWith(span, index, "INFINITY"))
            {
                value = sign * double.PositiveInfinity;
                index += 8;
                return true;
            }
            else if (StartsWith(span, index, "INF"))
            {
                value = sign * double.PositiveInfinity;
                index += 3;
                return true;
            }
            else
            {
                // No match
                value = default;
                return false;
            }
        }

        private struct DotSeparator : INumericTokens
        {
            public bool IsDecimalSeparator(char c)
            {
                return c == '.';
            }
        }
    }
}
