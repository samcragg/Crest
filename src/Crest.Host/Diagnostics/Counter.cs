// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    /// <summary>
    /// Allows the measuring of the number of times an event has occurred.
    /// </summary>
    internal sealed class Counter
    {
        /// <summary>
        /// Gets the current value of the counter.
        /// </summary>
        public long Value { get; private set; }

        /// <summary>
        /// Increments the counter by one.
        /// </summary>
        public void Increment()
        {
            this.Value++;
        }
    }
}
