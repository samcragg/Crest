// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using Crest.Host.Conversion;

    /// <summary>
    /// Converts an time duration to/from a series of bytes in the ISO 8601
    /// format.
    /// </summary>
    internal static class TimeSpanConverter
    {
        /// <summary>
        /// Represents the maximum number of characters a TimeSpan needs to be
        /// converted as text.
        /// </summary>
        public const int MaximumTextLength = 28; // P12345678DT12H12M12.1234567S

        /// <summary>
        /// Converts a time interval to human readable text.
        /// </summary>
        /// <param name="buffer">The byte array to output to.</param>
        /// <param name="offset">The index of where to start writing from.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The number of bytes written.</returns>
        public static int WriteTimeSpan(byte[] buffer, int offset, TimeSpan value)
        {
            long ticks = value.Ticks;
            if (ticks == 0)
            {
                buffer[offset] = (byte)'P';
                buffer[offset + 1] = (byte)'T';
                buffer[offset + 2] = (byte)'0';
                buffer[offset + 3] = (byte)'S';
                return 4;
            }
            else
            {
                int end;
                if (ticks > 0)
                {
                    end = WriteDuration(buffer, offset, (ulong)ticks);
                }
                else
                {
                    buffer[offset] = (byte)'-';
                    end = WriteDuration(buffer, offset + 1, (ulong)-ticks);
                }

                return end - offset;
            }
        }

        private static int AppendDurationPart(byte[] buffer, int index, int value, byte suffix)
        {
            if (value > 9)
            {
                buffer[index++] = (byte)PrimitiveDigits.Tens[value];
            }

            buffer[index++] = (byte)PrimitiveDigits.Units[value];
            buffer[index++] = suffix;

            return index;
        }

        private static int AppendFractionOfSeconds(byte[] buffer, int index, uint fractions)
        {
            // 1 tick == 100 nanoseconds == 0.0000001 seconds
            // Therefore, we need 7 decimal places, however, we're doing it in
            // pairs so we're doing it to 8 places and trimming the zeros
            index += 8;
            fractions *= 10; // 1 => 10 => 0.00 00 00 10
            for (int i = 0; i < (8 / 2); i++)
            {
                fractions = IntegerConverter.DivRem(fractions, 100, out int digits);
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

        private static int AppendTime(byte[] buffer, int index, ulong ticks)
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
                int seconds = (int)((ticks / TimeSpan.TicksPerSecond) % 60);
                index = AppendDurationPart(buffer, index, seconds, (byte)'S');

                uint fractionsOfSecond = (uint)(ticks % TimeSpan.TicksPerSecond);
                if (fractionsOfSecond > 0)
                {
                    buffer[index - 1] = (byte)'.'; // Overwrite the S suffix
                    index = AppendFractionOfSeconds(buffer, index, fractionsOfSecond);
                    buffer[index++] = (byte)'S';
                }
            }

            return index;
        }

        private static int WriteDuration(byte[] buffer, int index, ulong ticks)
        {
            buffer[index++] = (byte)'P';

            ulong days = ticks / TimeSpan.TicksPerDay;
            if (days != 0)
            {
                index += IntegerConverter.WriteUInt64(buffer, index, days);
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
