// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Contains helper methods used during parsing of numbers.
    /// </summary>
    internal static class NumberParsing
    {
        /// <summary>
        /// Calculates the quotient of two 64-bit unsigned integers and also
        /// returns the remainder in an output parameter.
        /// </summary>
        /// <param name="dividend">The dividend.</param>
        /// <param name="divisor">The divisor.</param>
        /// <param name="remainder">
        /// When this method returns, contains the remainder.
        /// </param>
        /// <returns>The quotient of the specified numbers.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong DivRem(ulong dividend, ulong divisor, out int remainder)
        {
            // See https://github.com/dotnet/coreclr/issues/3439 for why we use
            // multiplication instead of the modulus operator (basically this
            // avoids two divide machine instructions)
            ulong quotient = dividend / divisor;
            remainder = (int)(dividend - (quotient * divisor));
            return quotient;
        }

        /// <summary>
        /// Calculates the quotient of two 32-bit unsigned integers and also
        /// returns the remainder in an output parameter.
        /// </summary>
        /// <param name="dividend">The dividend.</param>
        /// <param name="divisor">The divisor.</param>
        /// <param name="remainder">
        /// When this method returns, contains the remainder.
        /// </param>
        /// <returns>The quotient of the specified numbers.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint DivRem(uint dividend, uint divisor, out int remainder)
        {
            // See https://github.com/dotnet/coreclr/issues/3439 for why we use
            // multiplication instead of the modulus operator (basically this
            // avoids two divide machine instructions)
            uint quotient = dividend / divisor;
            remainder = (int)(dividend - (quotient * divisor));
            return quotient;
        }
    }
}
