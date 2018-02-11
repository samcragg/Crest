// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    /// <content>
    /// Contains the nested <see cref="NumberInfo"/> interface.
    /// </content>
    internal static partial class DoubleConverter
    {
        /// <summary>
        /// Contains the information when parsing a number.
        /// </summary>
        internal struct NumberInfo
        {
            // IMPORTANT: See the note on DecimalConverter.NumberInfo
            // DO NOT RE-ORDER THE MEMBERS

            /// <summary>
            /// Represents the total number as if the decimal was not there.
            /// </summary>
            public ulong Significand;

            /// <summary>
            /// Gets the number of digits parsed.
            /// </summary>
            public ushort Digits;

            /// <summary>
            /// Gets the scale to raise the significand by.
            /// </summary>
            public short Scale;
        }
    }
}
