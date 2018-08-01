// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    /// <summary>
    /// Allows the displaying of a value.
    /// </summary>
    internal interface IUnit
    {
        /// <summary>
        /// Gets the description of the raw values passed to this instance to
        /// format.
        /// </summary>
        string ValueDescription { get; }

        /// <summary>
        /// Formats the specified value into a human readable form.
        /// </summary>
        /// <param name="value">The number to format.</param>
        /// <returns>A human readable value.</returns>
        string Format(long value);
    }
}