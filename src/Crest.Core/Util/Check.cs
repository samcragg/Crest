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
        /// Verifies the specified value is not null or empty.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="parameter">The name of the parameter.</param>
        public static void IsNotNullOrEmpty(string value, string parameter)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameter);
            }
            else if (value.Length == 0)
            {
                throw new ArgumentException("Value cannot be empty", parameter);
            }
        }
    }
}
