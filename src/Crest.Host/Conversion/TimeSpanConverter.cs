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
    /// Converts an time duration to/from a series of bytes in the ISO 8601
    /// format.
    /// </summary>
    internal static partial class TimeSpanConverter
    {
        /// <summary>
        /// Represents the maximum number of characters a TimeSpan needs to be
        /// converted as text.
        /// </summary>
        public const int MaximumTextLength = 28; // P12345678DT12H12M12.1234567S

        private const string InvalidFormat = "Invalid duration format, expected a format matching ISO 8601";
        private const int MaximumFractionDigits = 7; // TimeSpan.ToString default
        private const string OutOfRange = "The value is outside the valid range for a timespan";

        /// <summary>
        /// Reads a time interval from the buffer.
        /// </summary>
        /// <param name="span">Contains the characters to parse.</param>
        /// <returns>The result of the parsing operation.</returns>
        public static ParseResult<TimeSpan> TryReadTimeSpan(ReadOnlySpan<char> span)
        {
            int index = 0;
            int sign = NumberParsing.ParseSign(span, ref index);
            if (index == span.Length)
            {
                return new ParseResult<TimeSpan>(InvalidFormat);
            }

            if (IsEqualIgnoreCase(span[index], 'P'))
            {
                index++; // Skip the 'P'
                return ParseIsoDuration(span, ref index, sign);
            }
            else
            {
                return ParseNetDuration(span, ref index, sign);
            }
        }

        /// <summary>
        /// Converts a time interval to human readable text.
        /// </summary>
        /// <param name="buffer">The byte array to output to.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The number of bytes written.</returns>
        public static int WriteTimeSpan(in Span<byte> buffer, TimeSpan value)
        {
            long ticks = value.Ticks;
            if (ticks == 0)
            {
                buffer[0] = (byte)'P';
                buffer[1] = (byte)'T';
                buffer[2] = (byte)'0';
                buffer[3] = (byte)'S';
                return 4;
            }
            else if (ticks > 0)
            {
                return WriteDuration(buffer, (ulong)ticks);
            }
            else
            {
                buffer[0] = (byte)'-';
                return WriteDuration(buffer.Slice(1), (ulong)-ticks) + 1;
            }
        }

        private static int AppendDurationPart(in Span<byte> buffer, int index, int value, byte suffix)
        {
            if (value > 9)
            {
                buffer[index++] = (byte)PrimitiveDigits.Tens[value];
            }

            buffer[index++] = (byte)PrimitiveDigits.Units[value];
            buffer[index++] = suffix;

            return index;
        }

        private static int AppendFractionOfSeconds(in Span<byte> buffer, int index, uint fractions)
        {
            // 1 tick == 100 nanoseconds == 0.0000001 seconds
            // Therefore, we need 7 decimal places, however, we're doing it in
            // pairs so we're doing it to 8 places and trimming the zeros
            index += 8;
            fractions *= 10; // 1 => 10 => 0.00 00 00 10
            for (int i = 0; i < (8 / 2); i++)
            {
                fractions = NumberParsing.DivRem(fractions, 100, out int digits);
                buffer[--index] = (byte)PrimitiveDigits.Units[digits];
                buffer[--index] = (byte)PrimitiveDigits.Tens[digits];
            }

            // Find the last non-zero character
            index += 7;
            while (buffer[index] == (byte)'0')
            {
                index--;
            }

            return index + 1;
        }

        private static int AppendTime(in Span<byte> buffer, int index, ulong ticks)
        {
            buffer[index++] = (byte)'T';

            int hours = (int)((ticks / TimeSpan.TicksPerHour) % 24);
            if (hours > 0)
            {
                index = AppendDurationPart(buffer, index, hours, (byte)'H');
            }

            int minutes = (int)((ticks / TimeSpan.TicksPerMinute) % 60);
            if (minutes > 0)
            {
                index = AppendDurationPart(buffer, index, minutes, (byte)'M');
            }

            // Do we have any seconds?
            if ((ticks % TimeSpan.TicksPerMinute) != 0)
            {
                ulong totalSeconds = NumberParsing.DivRem(
                    ticks,
                    TimeSpan.TicksPerSecond,
                    out int fractionsOfSecond);

                int seconds = (int)(totalSeconds % 60);
                index = AppendDurationPart(buffer, index, seconds, (byte)'S');

                if (fractionsOfSecond > 0)
                {
                    buffer[index - 1] = (byte)'.'; // Overwrite the S suffix
                    index = AppendFractionOfSeconds(buffer, index, (uint)fractionsOfSecond);
                    buffer[index++] = (byte)'S';
                }
            }

            return index;
        }

        private static double GetDays(double years, double months, double days)
        {
            // Optimize for day only values
            if ((years == 0) && (months == 0))
            {
                return days;
            }

            // http://www.staff.science.uu.nl/~gent0113/calendar/isocalendar.htm
            // This computes the ISO day number since 0 January 0 CE
            //
            // The code on the website assumes the month is one based, so it's
            // been changes slightly to make this zero based. Also, the year 0
            // is a leap year, so it's a bit unexpected to have 1y == 366 days,
            // so we're doing the calculations on it being the year 1
            years++;
            if (months < 2)
            {
                years--;
                months += 12;
            }

            int yearsToDays =
                (int)Math.Floor(years * 365.25) -
                (int)Math.Floor(years / 100.0) +
                (int)Math.Floor(years / 400.0);

            int monthsToDays =
                (int)Math.Floor(30.6 * (months + 2));

            // Subtracting 366 days that are in year 0
            return yearsToDays + monthsToDays + days - (62 + 366);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsEqualIgnoreCase(char a, char b)
        {
            const int UpperToLower = 'a' - 'A';

            Assert(b == '\0' || char.IsUpper(b), "b must already be an uppercase letter");
            return (a == b) || (a == (b + UpperToLower));
        }

        private static bool ParseDigits(ReadOnlySpan<char> span, ref int index, ref string error, out int value)
        {
            ulong parsed = IntegerConverter.TryReadUInt64(span, ref index, ref error);
            if (parsed > int.MaxValue)
            {
                value = 0;
                error = OutOfRange;
                return false;
            }

            // Either it was out of range or a digit was expected, either way,
            // we can't continue
            if (error != null)
            {
                value = 0;
                return false;
            }

            value = (int)parsed;
            return true;
        }

        private static bool ParseDouble(ReadOnlySpan<char> span, ref int index, ref double value)
        {
            DoubleConverter.NumberInfo number = default;
            if (DoubleConverter.ParseSignificand<DecimalSeparator>(
                span,
                ref index,
                ref number))
            {
                // Improve the accuracy of the result for negative exponents
                if (number.Scale < 0)
                {
                    value = number.Significand / NumberParsing.Pow10(-number.Scale);
                }
                else
                {
                    value = number.Significand * NumberParsing.Pow10(number.Scale);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private static uint ParseFractions(ReadOnlySpan<char> span, ref int index, ref string error)
        {
            int digits = NumberParsing.ParseDigits(span, ref index, MaximumFractionDigits, out uint fraction);
            if (digits == 0)
            {
                error = InvalidFormat;
                return 0;
            }

            // Scale the number up (i.e. 0.003 becomes 3000)
            for (int i = digits; i < MaximumFractionDigits; i++)
            {
                fraction = fraction * 10;
            }

            return fraction;
        }

        private static ParseResult<TimeSpan> ParseIsoDuration(ReadOnlySpan<char> span, ref int index, int sign)
        {
            Triplet date = ParseTriplet(span, ref index, 'Y', 'M', 'D');
            Triplet time = default;
            if ((index < span.Length) && IsEqualIgnoreCase(span[index], 'T'))
            {
                index++;
                int startIndex = index;
                time = ParseTriplet(span, ref index, 'H', 'M', 'S');

                // We must parse part of the triplet if T is specified (i.e.
                // P0DT is invalid)
                if (index == startIndex)
                {
                    return new ParseResult<TimeSpan>(InvalidFormat);
                }
            }

            // The smallest valid value is P0D
            if (index < 3)
            {
                return new ParseResult<TimeSpan>(InvalidFormat);
            }

            double days = GetDays(date.First, date.Second, date.Third);
            long ticks =
                (long)(days * TimeSpan.TicksPerDay) +
                (long)(time.First * TimeSpan.TicksPerHour) +
                (long)(time.Second * TimeSpan.TicksPerMinute) +
                (long)(time.Third * TimeSpan.TicksPerSecond);

            return new ParseResult<TimeSpan>(TimeSpan.FromTicks(sign * ticks), index);
        }

        private static ParseResult<TimeSpan> ParseNetDuration(ReadOnlySpan<char> span, ref int index, int sign)
        {
            ParseResult<TimeSpan> CreateError(string e)
            {
                return new ParseResult<TimeSpan>(e ?? InvalidFormat);
            }

            string error = null;
            int days = 0;
            if (!ParseDigits(span, ref index, ref error, out int hours))
            {
                return CreateError(error);
            }

            if (ReadCharacter(span, ref index, '.'))
            {
                days = hours;
                if (!ParseDigits(span, ref index, ref error, out hours))
                {
                    return CreateError(error);
                }
            }

            if (!ReadCharacter(span, ref index, ':') ||
                !ParseDigits(span, ref index, ref error, out int minutes) ||
                !ReadCharacter(span, ref index, ':') ||
                !ParseDigits(span, ref index, ref error, out int seconds))
            {
                return CreateError(error);
            }

            long ticks =
                (days * TimeSpan.TicksPerDay) +
                (hours * TimeSpan.TicksPerHour) +
                (minutes * TimeSpan.TicksPerMinute) +
                (seconds * TimeSpan.TicksPerSecond);

            if (ReadCharacter(span, ref index, '.'))
            {
                ticks += ParseFractions(span, ref index, ref error);
                if (error != null)
                {
                    return CreateError(error);
                }
            }

            return new ParseResult<TimeSpan>(
                new TimeSpan(ticks * sign),
                index);
        }

        private static Triplet ParseTriplet(
            ReadOnlySpan<char> span,
            ref int index,
            char first,
            char second,
            char third)
        {
            Triplet triplet = default;
            int lastParsedValueStart = index;
            int remaining = 3;
            do
            {
                double parsed = 0;
                if (!ParseDouble(span, ref index, ref parsed))
                {
                    break;
                }

                // Check if there's a null character in the string, as we use
                // that character to clear parts we've already parsed
                if ((index >= span.Length) || (span[index] == '\0'))
                {
                    index = lastParsedValueStart;
                    break;
                }

                char c = span[index];
                if (IsEqualIgnoreCase(c, first))
                {
                    triplet.First = parsed;
                    first = '\0';
                    remaining = 2;
                }
                else if (IsEqualIgnoreCase(c, second))
                {
                    triplet.Second = parsed;
                    first = second = '\0';
                    remaining = 1;
                }
                else if (IsEqualIgnoreCase(c, third))
                {
                    triplet.Third = parsed;
                    first = second = third = '\0';
                    remaining = 0;
                }
                else
                {
                    index = lastParsedValueStart;
                    break;
                }

                // Skip the parsed character
                index++;
                lastParsedValueStart = index;
            }
            while (remaining != 0);

            return triplet;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ReadCharacter(ReadOnlySpan<char> span, ref int index, char expected)
        {
            if ((index >= span.Length) || (span[index] != expected))
            {
                return false;
            }
            else
            {
                index++;
                return true;
            }
        }

        private static int WriteDuration(in Span<byte> buffer, ulong ticks)
        {
            buffer[0] = (byte)'P';

            ulong days = ticks / TimeSpan.TicksPerDay;
            int index = 1;
            if (days != 0)
            {
                index += IntegerConverter.WriteUInt64(buffer.Slice(1), days);
                buffer[index++] = (byte)'D';
            }

            // If it's not a whole number of days then add the time part
            if (ticks > (days * TimeSpan.TicksPerDay))
            {
                index = AppendTime(buffer, index, ticks);
            }

            return index;
        }
    }
}
