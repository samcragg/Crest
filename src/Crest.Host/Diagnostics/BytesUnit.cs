// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    /// <summary>
    /// Represents a number of bytes.
    /// </summary>
    internal sealed class BytesUnit : IUnit
    {
        /// <inheritdoc />
        public string Format(long value)
        {
            if (value < 1024)
            {
                return $"{value} B";
            }
            else if (value < (1024 * 1024))
            {
                double amount = value / 1024.0;
                return $"{amount:f2} KiB";
            }
            else if (value < (1024 * 1024 * 1024))
            {
                double amount = value / (1024.0 * 1024.0);
                return $"{amount:f2} MiB";
            }
            else
            {
                double amount = value / (1024.0 * 1024.0 * 1024.0);
                return $"{amount:f2} GiB";
            }
        }
    }
}
