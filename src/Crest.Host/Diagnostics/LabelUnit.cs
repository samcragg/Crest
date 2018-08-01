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
    internal sealed class LabelUnit : IUnit
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LabelUnit"/> class.
        /// </summary>
        /// <param name="label">The text to append to the value.</param>
        public LabelUnit(string label)
        {
            this.ValueDescription = label;
        }

        /// <inheritdoc />
        public string ValueDescription { get; }

        /// <inheritdoc />
        public string Format(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture) + this.ValueDescription;
        }
    }
}
