// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Core.Util
{
    using System;

    /// <summary>
    /// Helper methods for argument verification.
    /// </summary>
    internal static class Check
    {
        /// <summary>
        /// Verifies the specified value is not null.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="parameter">The name of the parameter.</param>
        public static void IsNotNull(object value, string parameter)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameter);
            }
        }

        /// <summary>
        /// Verifies the specified value is not less than zero.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="parameter">The name of the parameter.</param>
        public static void IsPositive(int value, string parameter)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameter, parameter + " is less than 0.");
            }
        }
    }
}
