// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    /// <summary>
    /// Contains strings used during conversion of primitives to text.
    /// </summary>
    internal static class PrimitiveDigits
    {
        /// <summary>
        /// Represents the first 16 digits to use for hexadecimal values.
        /// </summary>
        public const string LowercaseHex = "0123456789abcdef";

        /// <summary>
        /// Represents the first character for digit pairs up to 99.
        /// </summary>
        public const string Tens = "0000000000111111111122222222223333333333444444444455555555556666666666777777777788888888889999999999";

        /// <summary>
        /// Represents the second character for digit pairs up to 99.
        /// </summary>
        public const string Units = "0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789";

        /// <summary>
        /// Represents the first 16 digits to use for hexadecimal values.
        /// </summary>
        public const string UppercaseHex = "0123456789ABCDEF";
    }
}
