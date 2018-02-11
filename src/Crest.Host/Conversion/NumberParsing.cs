// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Contains helper methods used during parsing of numbers.
    /// </summary>
    internal static class NumberParsing
    {
        // We use 18 as that's the maximum amount of significant digits for a
        // double - if it's greater we'll fall back to Math.Pow
        private static readonly double[] PowersOf10 =
        {
            1,
            1e+1,
            1e+2,
            1e+3,
            1e+4,
            1e+5,
            1e+6,
            1e+7,
            1e+8,
            1e+9,
            1e+10,
            1e+11,
            1e+12,
            1e+13,
            1e+14,
            1e+15,
            1e+16,
            1e+17,
            1e+18,
        };

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

        /// <summary>
        /// Raises the number 10 to the specified power.
        /// </summary>
        /// <param name="power">Specifies the power.</param>
        /// <returns>The number 10 raised to the specified power.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static double Pow10(int power)
        {
            if (power <= 18)
            {
                return PowersOf10[power];
            }
            else
            {
                return Math.Pow(10, power);
            }
        }
    }
}
