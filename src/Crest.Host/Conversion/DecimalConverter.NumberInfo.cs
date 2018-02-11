// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    /// <content>
    /// Contains the nested <see cref="NumberInfo"/> struct.
    /// </content>
    internal static partial class DecimalConverter
    {
        // IMPORTANT:
        //
        // The order of the fields matters. Larger fields MUST be placed before
        // smaller fields.
        //
        // This is to avoid unnecessary packing bytes, i.e. given the following:
        //
        //    ushort Digits
        //    ulong Hi
        //    ushort Scale
        //
        // This would take 24 bytes but rearranging with large fields first:
        //
        //    ulong Hi
        //    ushort Digits
        //    ushort Scale
        //
        // Gives 16 bytes. Although memory is cheep, because this is a struct
        // it gets copied when passing it around, so we can save some
        // instructions by keeping its size small.
        private struct NumberInfo
        {
            public ulong Hi;

            public ulong Lo;

            public ushort Digits;

            public ushort IntegerDigits;

            public short Scale;
        }
    }
}
