// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;
    using System.Runtime.InteropServices;

    /// <content>
    /// Contains the nested <see cref="GuidBytes"/> struct.
    /// </content>
    internal static partial class GuidConverter
    {
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
