// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System.Diagnostics;

    /// <summary>
    /// Allows the querying of the current system time.
    /// </summary>
    internal class TimeProvider : ITimeProvider
    {
        private readonly double ticksPerMicrosecond = Stopwatch.Frequency / (1000.0 * 1000.0);

        /// <inheritdoc />
        public long GetCurrentMicroseconds()
        {
            return (long)(Stopwatch.GetTimestamp() / this.ticksPerMicrosecond);
        }
    }
}
