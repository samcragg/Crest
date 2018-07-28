// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System.Globalization;

    /// <summary>
    /// Represents a number of bytes.
    /// </summary>
    internal sealed class BytesUnit : IUnit
    {
        /// <summary>
        /// Gets an instance of this class.
        /// </summary>
        internal static BytesUnit Instance { get; } = new BytesUnit();

        /// <inheritdoc />
        public string Format(long value)
        {
            if (value < 1024)
            {
                return value.ToString(NumberFormatInfo.InvariantInfo) + " B";
            }
            else if (value < (1024 * 1024))
            {
                double amount = value / 1024.0;
                return amount.ToString("f2", NumberFormatInfo.InvariantInfo) + " KiB";
            }
            else if (value < (1024 * 1024 * 1024))
            {
                double amount = value / (1024.0 * 1024.0);
                return amount.ToString("f2", NumberFormatInfo.InvariantInfo) + " MiB";
            }
            else
            {
                double amount = value / (1024.0 * 1024.0 * 1024.0);
                return amount.ToString("f2", NumberFormatInfo.InvariantInfo) + " GiB";
            }
        }
    }
}
