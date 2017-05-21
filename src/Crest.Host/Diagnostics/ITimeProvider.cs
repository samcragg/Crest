// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System;

    /// <summary>
    /// Allows the querying of the current system time.
    /// </summary>
    internal interface ITimeProvider
    {
        /// <summary>
        /// Gets the number of microseconds in the system timer mechanism.
        /// </summary>
        /// <returns>
        /// The number of microseconds in the underlying timer mechanism.
        /// </returns>
        long GetCurrentMicroseconds();

        /// <summary>
        /// Gets the current date and time on this computer, expressed as the
        /// Coordinated Universal Time (UTC).
        /// </summary>
        /// <returns>
        /// An object whose value is the current UTC date and time.
        /// </returns>
        DateTime GetUtc();
    }
}
