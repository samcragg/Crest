// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess.Parsing
{
    /// <summary>
    /// Represents the method to filter the results by.
    /// </summary>
    internal enum FilterMethod
    {
        // -------
        // Generic
        // -------

        /// <summary>
        /// Filter results to match the value.
        /// </summary>
        Equals = 0x01,

        /// <summary>
        /// Filter the value to be one of a list of values.
        /// </summary>
        In = 0x02,

        /// <summary>
        /// Filter results that do not match the value.
        /// </summary>
        NotEquals = 0x03,

        // ----------
        // Arithmetic
        // ----------

        /// <summary>
        /// Filter results to be greater than the value.
        /// </summary>
        GreaterThan = 0x11,

        /// <summary>
        /// Filter results to be greater than or equal to the value.
        /// </summary>
        GreaterThanOrEqual = 0x12,

        /// <summary>
        /// Filter results to be less than the value.
        /// </summary>
        LessThan = 0x13,

        /// <summary>
        /// Filter results to be less than or equal to the value.
        /// </summary>
        LessThanOrEqual = 0x14,

        // ------
        // String
        // ------

        /// <summary>
        /// Filter results to those that contain the value.
        /// </summary>
        Contains = 0x21,

        /// <summary>
        /// Filter results to those that end with the value.
        /// </summary>
        EndsWith = 0x22,

        /// <summary>
        /// Filter results to those that start with the value.
        /// </summary>
        StartsWith = 0x23,
    }
}
