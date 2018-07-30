// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System.Globalization;

    /// <summary>
    /// Represents units of time.
    /// </summary>
    /// <remarks>
    /// The input value to format must be in microseconds.
    /// </remarks>
    internal sealed class TimeUnit : IUnit
    {
        /// <summary>
        /// Gets an instance of this class.
        /// </summary>
        internal static TimeUnit Instance { get; } = new TimeUnit();

        /// <inheritdoc />
        public string Format(long value)
        {
            if (value <= 0)
            {
                return "0";
            }
            else if (value < 1000)
            {
                return value.ToString(NumberFormatInfo.InvariantInfo) + "µs";
            }
            else if (value < (1000 * 1000))
            {
                return (value / 1_000.0).ToString("f1", NumberFormatInfo.InvariantInfo) + "ms";
            }
            else
            {
                return (value / 1_000_000.0).ToString("f1", NumberFormatInfo.InvariantInfo) + "s";
            }
        }
    }
}
