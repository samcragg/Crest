// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;

    /// <summary>
    /// Converts an date time to/from a series of bytes in the ISO 8601 format.
    /// </summary>
    internal static class DateTimeConverter
    {
        /// <summary>
        /// Represents the maximum number of characters a DateTime needs to be
        /// converted as text.
        /// </summary>
        public const int MaximumTextLength = 28; // 1234-67-90T23:56:89.1234567Z

        private const int DatePartLength = 10;
        private const string InvalidDateFormat = "Invalid date format, expected a format matching ISO 8601";
        private const int MaximumFractionPrecision = 7; // One tick == 0.0000001 seconds
        private const string OutsideOfRange = "The value describes an un-representable date/time";

        /// <summary>
        /// Reads a date and optional time from the specified value.
        /// </summary>
        /// <param name="span">Contains the characters to parse.</param>
        /// <returns>The result of the parsing operation.</returns>
        public static ParseResult<DateTime> TryReadDateTime(ReadOnlySpan<char> span)
        {
            int index = 0;
            string error = null;
            DateTime date = ReadDate(span, ref index, ref error);
            if (error != null)
            {
                return new ParseResult<DateTime>(error);
            }

            TimeSpan time = TimeSpan.Zero;
            if (ReadChar(span, ref index, 'T'))
            {
                time = ReadTime(span, ref index, ref error);
                if (error != null)
                {
                    return new ParseResult<DateTime>(error);
                }

                TimeSpan offset = ReadTimeZone(span, ref index, ref error);
                if (error != null)
                {
                    return new ParseResult<DateTime>(error);
                }

                time = time - offset;
            }

            return new ParseResult<DateTime>(date.Add(time), index);
        }

        /// <summary>
        /// Converts a date time value to human readable text.
        /// </summary>
        /// <param name="buffer">The byte array to output to.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The number of bytes written.</returns>
        public static int WriteDateTime(in Span<byte> buffer, DateTime value)
        {
            const int TimePartLength = 9;
            AppendDate(buffer, value.Year, value.Month, value.Day);
            AppendTime(buffer, value.Hour, value.Minute, value.Second);

            int index = DatePartLength + TimePartLength;
            int fraction = (int)(value.Ticks % TimeSpan.TicksPerSecond);
            if (fraction > 0)
            {
                buffer[index++] = (byte)'.';
                index += AppendFractionsOfSeconds(buffer, index, fraction);
            }

            buffer[index++] = (byte)'Z';
            return index;
        }

        private static void AppendDate(in Span<byte> buffer, int year, int month, int day)
        {
            int pair = year / 100;
            buffer[0] = (byte)PrimitiveDigits.Tens[pair];
            buffer[1] = (byte)PrimitiveDigits.Units[pair];
            pair = year % 100;
            buffer[2] = (byte)PrimitiveDigits.Tens[pair];
            buffer[3] = (byte)PrimitiveDigits.Units[pair];

            buffer[4] = (byte)'-';
            buffer[5] = (byte)PrimitiveDigits.Tens[month];
            buffer[6] = (byte)PrimitiveDigits.Units[month];

            buffer[7] = (byte)'-';
            buffer[8] = (byte)PrimitiveDigits.Tens[day];
            buffer[9] = (byte)PrimitiveDigits.Units[day];
        }

        private static int AppendFractionsOfSeconds(Span<byte> buffer, int offset, int value)
        {
            buffer[offset] = (byte)PrimitiveDigits.Units[value / 1000000];

            int pair = (value / 10000) % 100;
            buffer[offset + 1] = (byte)PrimitiveDigits.Tens[pair];
            buffer[offset + 2] = (byte)PrimitiveDigits.Units[pair];

            pair = (value / 100) % 100;
            buffer[offset + 3] = (byte)PrimitiveDigits.Tens[pair];
            buffer[offset + 4] = (byte)PrimitiveDigits.Units[pair];

            pair = value % 100;
            buffer[offset + 5] = (byte)PrimitiveDigits.Tens[pair];
            buffer[offset + 6] = (byte)PrimitiveDigits.Units[pair];

            return 7;
        }

        private static void AppendTime(in Span<byte> buffer, int hour, int minute, int second)
        {
            buffer[DatePartLength] = (byte)'T';
            buffer[DatePartLength + 1] = (byte)PrimitiveDigits.Tens[hour];
            buffer[DatePartLength + 2] = (byte)PrimitiveDigits.Units[hour];

            buffer[DatePartLength + 3] = (byte)':';
            buffer[DatePartLength + 4] = (byte)PrimitiveDigits.Tens[minute];
            buffer[DatePartLength + 5] = (byte)PrimitiveDigits.Units[minute];

            buffer[DatePartLength + 6] = (byte)':';
            buffer[DatePartLength + 7] = (byte)PrimitiveDigits.Tens[second];
            buffer[DatePartLength + 8] = (byte)PrimitiveDigits.Units[second];
        }

        private static bool ReadChar(in ReadOnlySpan<char> span, ref int index, char expected)
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

        private static DateTime ReadDate(ReadOnlySpan<char> span, ref int index, ref string error)
        {
            uint month = 0, day = 0;
            bool valid = false;
            if (ReadInteger(span, ref index, out uint year))
            {
                if (index == 4)
                {
                    // Extended form
                    valid =
                        ReadChar(span, ref index, '-') &&
                        ReadPair(span, ref index, out month) &&
                        ReadChar(span, ref index, '-') &&
                        ReadPair(span, ref index, out day);
                }
                else if (index == 8)
                {
                    // Basic form: year should represent yyyyMMdd
                    valid = true;
                    year = NumberParsing.DivRem(year, 10000, out int remaining);
                    month = NumberParsing.DivRem((uint)remaining, 100, out remaining);
                    day = (uint)remaining;
                }
            }

            if (!valid)
            {
                error = InvalidDateFormat;
                return default;
            }

            // No need to optimize the unhappy path, simple handle the exception
            // as the DateTime does the checks for us anyway, even if we've
            // validated the values are correct
            try
            {
                return new DateTime((int)year, (int)month, (int)day, 0, 0, 0, DateTimeKind.Utc);
            }
            catch (ArgumentOutOfRangeException)
            {
                error = OutsideOfRange;
                return default;
            }
        }

        private static bool ReadFractionsOfSeconds(ReadOnlySpan<char> span, ref int index, out uint fractions)
        {
            // Fractions are optional, therefore, only return false if it should
            // be there but isn't (i.e. if we're at the end of the string or
            // there's no decimal separator then it's not an error)
            if (index >= span.Length)
            {
                fractions = 0;
                return true;
            }

            char c = span[index];
            if ((c != '.') && (c != ','))
            {
                fractions = 0;
                return true;
            }

            index++;
            int start = index;
            if (!ReadInteger(span, ref index, out fractions))
            {
                return false;
            }

            int length = index - start;
            while (length > MaximumFractionPrecision)
            {
                fractions /= 10;
                length--;
            }

            uint power = (uint)NumberParsing.Pow10(MaximumFractionPrecision - length);
            fractions = fractions * power;
            return true;
        }

        private static bool ReadInteger(in ReadOnlySpan<char> span, ref int index, out uint result)
        {
            if (index >= span.Length)
            {
                result = 0;
                return false;
            }

            uint digit = (uint)(span[index] - '0');
            if (digit > 9)
            {
                result = 0;
                return false;
            }

            result = digit;
            while (++index < span.Length)
            {
                digit = (uint)(span[index] - '0');
                if (digit > 9)
                {
                    break;
                }

                result = (result * 10u) + digit;
            }

            return true;
        }

        private static bool ReadPair(in ReadOnlySpan<char> span, ref int index, out uint result)
        {
            if (index <= span.Length - 2)
            {
                uint tens = (uint)(span[index] - '0');
                uint units = (uint)(span[index + 1] - '0');
                if ((tens < 10) && (units < 10))
                {
                    result = (tens * 10u) + units;
                    index += 2;
                    return true;
                }
            }

            result = 0;
            return false;
        }

        private static TimeSpan ReadTime(ReadOnlySpan<char> span, ref int index, ref string error)
        {
            uint minute = 0, second = 0;
            bool valid = false;
            int start = index;
            if (ReadInteger(span, ref index, out uint hour))
            {
                int read = index - start;
                if (read == 2)
                {
                    // Extended form
                    valid =
                        ReadChar(span, ref index, ':') &&
                        ReadPair(span, ref index, out minute) &&
                        ReadChar(span, ref index, ':') &&
                        ReadPair(span, ref index, out second);
                }
                else if (read == 6)
                {
                    // Basic form: hour represents hhmmmss
                    valid = true;
                    hour = NumberParsing.DivRem(hour, 10000, out int remaining);
                    minute = NumberParsing.DivRem((uint)remaining, 100, out remaining);
                    second = (uint)remaining;
                }
            }

            if (!valid || !ReadFractionsOfSeconds(span, ref index, out uint fractions))
            {
                error = InvalidDateFormat;
                return default;
            }

            // We need to allow 24:00:00 so we can't validate the hour just yet
            // - wait till we have the total and then validate it
            if ((minute < 60) && (second < 60))
            {
                long ticks =
                    fractions +
                    (second * TimeSpan.TicksPerSecond) +
                    (minute * TimeSpan.TicksPerMinute) +
                    (hour * TimeSpan.TicksPerHour);

                if (ticks <= (TimeSpan.TicksPerHour * 24))
                {
                    return TimeSpan.FromTicks(ticks);
                }
            }

            error = OutsideOfRange;
            return default;
        }

        private static TimeSpan ReadTimeZone(ReadOnlySpan<char> span, ref int index, ref string error)
        {
            if (index >= span.Length)
            {
                // Assume none specified
                return TimeSpan.Zero;
            }

            // The timezone must start with a '+' or '-', unless it's UTC which
            // can be 'Z'
            int sign = 0;
            switch (span[index++])
            {
                case '+':
                    sign = 1;
                    break;

                case '-':
                    sign = -1;
                    break;

                case 'z':
                case 'Z':
                    return TimeSpan.Zero; // UTC

                default:
                    error = InvalidDateFormat;
                    return default;
            }

            uint minute = 0;
            if (!ReadPair(span, ref index, out uint hour))
            {
                error = InvalidDateFormat;
                return default;
            }

            // The format can either be hh, hh:mm or hhmm
            if (index < span.Length)
            {
                if (span[index] == ':')
                {
                    index++;
                }

                if (!ReadPair(span, ref index, out minute))
                {
                    error = InvalidDateFormat;
                    return default;
                }
            }

            return new TimeSpan((int)hour * sign, (int)minute * sign, 0);
        }
    }
}
