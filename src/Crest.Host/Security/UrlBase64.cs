// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System.Runtime.CompilerServices;
    using Crest.Core.Logging;

    /// <summary>
    /// Allows the handling of Base 64 URL encoded data.
    /// </summary>
    internal static class UrlBase64
    {
        private const byte InvalidCharacater = byte.MaxValue;
        private static readonly ILog Logger = Log.For(typeof(UrlBase64));
        private static readonly byte[] Lookup = new byte[128];

        static UrlBase64()
        {
            const string Base64CharSet = @"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

            // Fill the buffer with marker values so we know if we get an
            // invalid Base64 character when decoding
            for (int i = 0; i < Lookup.Length; i++)
            {
                Lookup[i] = InvalidCharacater;
            }

            for (int i = 0; i < 64; i++)
            {
                Lookup[Base64CharSet[i]] = (byte)i;
            }
        }

        /// <summary>
        /// Converts the specified base-64 URL encoded string to an equivalent
        /// 8-bit unsigned integer array.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <param name="start">
        /// The zero-based starting character position of the data to decode.
        /// </param>
        /// <param name="end">
        /// The zero-based end character position of the data to decode.
        /// </param>
        /// <param name="buffer">
        /// When this method returns, will contain the an array of 8-bit
        /// unsigned integers.
        /// </param>
        /// <returns>
        /// <c>true</c> if the conversion was successful; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryDecode(string str, int start, int end, out byte[] buffer)
        {
            int length = ((end - start) * 3) / 4;
            buffer = new byte[length];
            int index = 0;
            int bits = -8;
            int value = 0;
            int b = 0;

            for (int i = start; i < end; i++)
            {
                if (!DecodeBase64Char(str[i], ref b))
                {
                    Logger.InfoFormat("Invalid URL base 64 char at {index}: '{char}'", i, str[i]);
                    return false;
                }

                // Based on https://stackoverflow.com/a/34571089/312325
                value = (value << 6) | b;
                bits += 6;
                if (bits >= 0)
                {
                    buffer[index++] = (byte)(value >> bits);
                    bits -= 8;
                }
            }

            return true;
        }

        // 3x performance boost by inlining method
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DecodeBase64Char(char c, ref int b)
        {
            if (c < Lookup.Length)
            {
                b = Lookup[c];
                return b != InvalidCharacater;
            }

            return false;
        }
    }
}
