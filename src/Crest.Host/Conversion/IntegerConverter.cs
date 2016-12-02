// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    /// <summary>
    /// Allows the quick conversion of integers to strings (and vice versa).
    /// </summary>
    internal static class IntegerConverter
    {
        /// <summary>
        /// Parses an integer number without a sign.
        /// </summary>
        /// <param name="segment">The string to parse.</param>
        /// <param name="result">Will contain the parsed value.</param>
        /// <returns>
        /// <c>true</c> if an integer was parsed; otherwise, <c>false</c>.
        /// </returns>
        internal static bool ParseIntegerValue(StringSegment segment, out long result)
        {
            ulong value = 0;
            if (ParseInteger(segment, 0, ref value))
            {
                result = (long)value;
                return true;
            }
            else
            {
                // If it failed value will be garbage, hence the need to check
                // instead of blindly trusting ParseInteger and unconditionally
                // casting value and returning the result from ParseInteger
                result = 0;
                return false;
            }
        }

        /// <summary>
        /// Parses an integer number that is optionally prefixed with a sign.
        /// </summary>
        /// <param name="segment">The string to parse.</param>
        /// <param name="result">Will contain the parsed value.</param>
        /// <returns>
        /// <c>true</c> if an integer was parsed; otherwise, <c>false</c>.
        /// </returns>
        internal static bool ParseSignedValue(StringSegment segment, out long result)
        {
            int index = 0;
            long sign = ParseSign(segment, ref index);
            ulong integer = 0;
            if (ParseInteger(segment, index, ref integer))
            {
                result = (long)integer * sign;
                return true;
            }
            else
            {
                result = 0;
                return false;
            }
        }

        private static bool ParseInteger(StringSegment segment, int index, ref ulong value)
        {
            for (; index < segment.Count; index++)
            {
                char c = segment[index];
                uint digit = (uint)(c - '0');
                if (digit > 10)
                {
                    return false;
                }

                value = (value * 10u) + digit;
            }

            return true;
        }

        private static long ParseSign(StringSegment segment, ref int index)
        {
            char c = segment[0];
            if (c == '-')
            {
                index = 1;
                return -1L;
            }
            else if (c == '+')
            {
                index = 1;
                return 1L;
            }
            else
            {
                return 1L;
            }
        }
    }
}
