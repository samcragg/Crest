// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Converts double-precision floating-point number to/from a series of
    /// ASCII bytes.
    /// </summary>
    internal static partial class DecimalConverter
    {
        private const string InvalidFormat = "Invalid format";
        private const int MaxExponentDigits = 2;
        private const int MaximumScale = 28;
        private const int MaximumSignificantDigitsForDecimal = 29;
        private const int MaximumSignificantDigitsForLong = 20 - 1; // -1 so that we don't overflow
        private const string OutsideRange = "The value is outside the valid decimal range";

        /// <summary>
        /// Reads a double from the specified value.
        /// </summary>
        /// <param name="span">Contains the characters to parse.</param>
        /// <returns>The result of the parsing operation.</returns>
        public static ParseResult<decimal> TryReadDecimal(ReadOnlySpan<char> span)
        {
            string error = null;
            int index = 0;
            int sign = NumberParsing.ParseSign(span, ref index);
            int startOfDigits = index;

            NumberInfo number = default;
            ParseSignificand(span, ref index, ref number);

            // Have we parsed a number?
            if (index != startOfDigits)
            {
                number.Scale += (short)NumberParsing.ParseExponent(span, ref index);
                decimal value = MakeDecimal(ref number, sign, ref error);
                if (error == null)
                {
                    return new ParseResult<decimal>(value, index);
                }
            }
            else
            {
                error = InvalidFormat;
            }

            return new ParseResult<decimal>(error);
        }

        private static decimal MakeDecimal(ref NumberInfo number, int sign, ref string error)
        {
            if (number.IntegerDigits <= MaximumSignificantDigitsForDecimal)
            {
                if (number.Scale <= 0)
                {
                    if (number.Scale >= -MaximumScale)
                    {
                        return new decimal(
                            (int)number.Lo,
                            (int)(number.Lo >> 32),
                            (int)number.Hi,
                            sign < 0,
                            (byte)-number.Scale);
                    }
                }
                else
                {
                    // We need to make sure that the new number will fit within
                    // our precision (e.g. 1e30 is too big)
                    if ((number.IntegerDigits + number.Scale) <= MaximumSignificantDigitsForDecimal)
                    {
                        Pow10(ref number, number.Scale);

                        // Final check for overflow
                        if (number.Hi <= uint.MaxValue)
                        {
                            return new decimal(
                                (int)number.Lo,
                                (int)(number.Lo >> 32),
                                (int)number.Hi,
                                sign < 0,
                                0);
                        }
                    }
                }
            }

            error = OutsideRange;
            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MultiplyByTen(ref NumberInfo number)
        {
            // We're relying on the fact that we can multiply by 10 using
            // bit shifting:
            //     10x = 8x + 2x = (x << 3) + (x << 1)
            // Using the above, we need to move the top bits of lo to hi
            //     ((lo << 64) >> 3) + ((lo << 64) >> 1)
            number.Hi *= 10;
            number.Hi += (number.Lo >> 61) + (number.Lo >> 63);

            // Check if adding them together in lo will carry over
            ulong x8 = number.Lo << 3;
            ulong x2 = number.Lo << 1;
            if ((ulong.MaxValue - x8) < x2)
            {
                number.Hi++;
            }

            number.Lo = x8 + x2;
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

                if (number.Digits >= MaximumSignificantDigitsForLong)
                {
                    // We need to add to both hi and lo now
                    ParseDigitsWithOverflow(span, ref index, ref number, true);
                    break;
                }

                number.Digits++;
                number.Lo = (number.Lo * 10) + digit;
            }
        }

        private static void ParseDigitsWithOverflow(ReadOnlySpan<char> span, ref int index, ref NumberInfo number, bool canRound)
        {
            bool skipDigits = false;
            for (; index < span.Length; index++)
            {
                uint digit = (uint)(span[index] - '0');
                if (digit > 9)
                {
                    break;
                }

                // We've already skipped leading zero's before this method was
                // called, so each digit is significant
                number.Digits++;
                if (skipDigits)
                {
                    number.Scale++;
                    continue;
                }

                ulong originalHi = number.Hi;
                ulong originalLo = number.Lo;
                MultiplyByTen(ref number);

                // Easier to check for wrap around here, as the maximum we're
                // adding will be 9
                ulong oldLo = number.Lo;
                number.Lo += digit;
                if (number.Lo < oldLo)
                {
                    number.Hi++;
                }

                // Check to see if we've exceeded the maximum value
                if (number.Hi > uint.MaxValue)
                {
                    // Perform some rounding. Assuming the maximum is 335, if
                    // we parse 336 we should turn it to 34e1 and 351 to 35e1,
                    // hence the need to increase the scale
                    number.Hi = originalHi;
                    number.Lo = originalLo;
                    number.Scale++;
                    RoundNumber(ref number, digit);

                    // We're not going to use any more digits
                    skipDigits = true;
                }
            }
        }

        private static void ParseSignificand(ReadOnlySpan<char> span, ref int index, ref NumberInfo number)
        {
            int originalIndex = index;
            ParseDigits(span, ref index, ref number);
            int digits = index - originalIndex;

            // Is there a decimal part as well?
            if ((index < span.Length) && (span[index] == '.'))
            {
                index++; // Skip the '.'

                number.IntegerDigits = number.Digits;
                ParseDigits(span, ref index, ref number);
                number.Scale += (short)(number.IntegerDigits - number.Digits);

                // Check it's not just a decimal point
                if ((index - originalIndex) == 1)
                {
                    index = originalIndex;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Pow10(ref NumberInfo number, int amount)
        {
            for (; amount > 0; amount--)
            {
                MultiplyByTen(ref number);
            }
        }

        private static void RoundNumber(ref NumberInfo number, uint digit)
        {
            // Is there any rounding to do?
            if (digit < 5)
            {
                return;
            }

            // Can we increase the number without it overflowing?
            if (number.Lo == ulong.MaxValue)
            {
                if (number.Hi == uint.MaxValue)
                {
                    // Can't increase so reduce accuracy
                    number.Lo = 0x999999999999999Au;
                    number.Hi = 0x19999999u;
                    number.Scale++;
                }
                else
                {
                    number.Lo = 0;
                    number.Hi++;
                }
            }
            else
            {
                number.Lo++;
            }
        }
    }
}
