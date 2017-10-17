// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
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

        /// <summary>
        /// Converts a date time value to human readable text.
        /// </summary>
        /// <param name="buffer">The byte array to output to.</param>
        /// <param name="offset">The index of where to start writing from.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The number of bytes written.</returns>
        public static int WriteDateTime(byte[] buffer, int offset, DateTime value)
        {
            int index = offset;
            index += AppendDate(buffer, index, value.Year, value.Month, value.Day);

            buffer[index++] = (byte)'T';
            index += AppendTime(buffer, index, value.Hour, value.Minute, value.Second);

            int fraction = (int)(value.Ticks % TimeSpan.TicksPerSecond);
            if (fraction > 0)
            {
                buffer[index++] = (byte)'.';
                index += AppendFractionsOfSeconds(buffer, index, fraction);
            }

            buffer[index++] = (byte)'Z';
            return index - offset;
        }

        private static int AppendDate(byte[] buffer, int offset, int year, int month, int day)
        {
            int pair = year / 100;
            buffer[offset] = (byte)PrimitiveDigits.Tens[pair];
            buffer[offset + 1] = (byte)PrimitiveDigits.Units[pair];
            pair = year % 100;
            buffer[offset + 2] = (byte)PrimitiveDigits.Tens[pair];
            buffer[offset + 3] = (byte)PrimitiveDigits.Units[pair];

            buffer[offset + 4] = (byte)'-';
            buffer[offset + 5] = (byte)PrimitiveDigits.Tens[month];
            buffer[offset + 6] = (byte)PrimitiveDigits.Units[month];

            buffer[offset + 7] = (byte)'-';
            buffer[offset + 8] = (byte)PrimitiveDigits.Tens[day];
            buffer[offset + 9] = (byte)PrimitiveDigits.Units[day];

            return 10;
        }

        private static int AppendFractionsOfSeconds(byte[] buffer, int offset, int value)
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

        private static int AppendTime(byte[] buffer, int offset, int hour, int minute, int second)
        {
            buffer[offset] = (byte)PrimitiveDigits.Tens[hour];
            buffer[offset + 1] = (byte)PrimitiveDigits.Units[hour];

            buffer[offset + 2] = (byte)':';
            buffer[offset + 3] = (byte)PrimitiveDigits.Tens[minute];
            buffer[offset + 4] = (byte)PrimitiveDigits.Units[minute];

            buffer[offset + 5] = (byte)':';
            buffer[offset + 6] = (byte)PrimitiveDigits.Tens[second];
            buffer[offset + 7] = (byte)PrimitiveDigits.Units[second];

            return 8;
        }
    }
}
