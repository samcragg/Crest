// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
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
    }
}
