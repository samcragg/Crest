// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Used to convert a <see cref="Guid"/> to/from a series of ASCII bytes.
    /// </summary>
    internal static class GuidConverter
    {
        /// <summary>
        /// Represents the maximum number of characters a GUID needs to be
        /// converted as text.
        /// </summary>
        public const int MaximumTextLength = 36;

        /// <summary>
        /// Converts a date time value to human readable text.
        /// </summary>
        /// <param name="buffer">The byte array to output to.</param>
        /// <param name="offset">The index of where to start writing from.</param>
        /// <param name="value">The value to convert.</param>
        /// <returns>The number of bytes written.</returns>
        public static int WriteGuid(byte[] buffer, int offset, Guid value)
        {
            // Quicker than ToByteArray + saves memory allocation
            var bytes = default(GuidBytes);
            bytes.Guid = value;

            // The format will be: "12345678-0123-5678-0123-567890123456"
            buffer[offset + 8] = (byte)'-';
            buffer[offset + 13] = (byte)'-';
            buffer[offset + 18] = (byte)'-';
            buffer[offset + 23] = (byte)'-';

            // NOTE: The GUID stores the first few bytes as integers/shorts in
            // the following format:
            //     private int _a;
            //     private short _b;
            //     private short _c;
            // Therefore, switch the first few bytes around
            // int _a
            WriteHexPair(buffer, offset + 0, bytes.B3, bytes.B2);
            WriteHexPair(buffer, offset + 4, bytes.B1, bytes.B0);

            // short _b
            WriteHexPair(buffer, offset + 9, bytes.B5, bytes.B4);

            // short _c
            WriteHexPair(buffer, offset + 14, bytes.B7, bytes.B6);

            // Back to bytes (e.g. byte _d)
            WriteHexPair(buffer, offset + 19, bytes.B8, bytes.B9);
            WriteHexPair(buffer, offset + 24, bytes.B10, bytes.B11);
            WriteHexPair(buffer, offset + 28, bytes.B12, bytes.B13);
            WriteHexPair(buffer, offset + 32, bytes.B14, bytes.B15);

            return MaximumTextLength;
        }

        private static void WriteHexPair(byte[] buffer, int offset, byte a, byte b)
        {
            buffer[offset] = (byte)PrimitiveDigits.LowercaseHex[a >> 4];
            buffer[offset + 1] = (byte)PrimitiveDigits.LowercaseHex[a & 0xF];
            buffer[offset + 2] = (byte)PrimitiveDigits.LowercaseHex[b >> 4];
            buffer[offset + 3] = (byte)PrimitiveDigits.LowercaseHex[b & 0xF];
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct GuidBytes
        {
            [FieldOffset(0)]
            public Guid Guid;

            [FieldOffset(0)]
            public byte B0;

            [FieldOffset(1)]
            public byte B1;

            [FieldOffset(2)]
            public byte B2;

            [FieldOffset(3)]
            public byte B3;

            [FieldOffset(4)]
            public byte B4;

            [FieldOffset(5)]
            public byte B5;

            [FieldOffset(6)]
            public byte B6;

            [FieldOffset(7)]
            public byte B7;

            [FieldOffset(8)]
            public byte B8;

            [FieldOffset(9)]
            public byte B9;

            [FieldOffset(10)]
            public byte B10;

            [FieldOffset(11)]
            public byte B11;

            [FieldOffset(12)]
            public byte B12;

            [FieldOffset(13)]
            public byte B13;

            [FieldOffset(14)]
            public byte B14;

            [FieldOffset(15)]
            public byte B15;
        }
    }
}
