// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System.Threading;

    /// <summary>
    /// Allows the measuring of the number of times an event has occurred.
    /// </summary>
    internal sealed class Counter
    {
        private long value;

        /// <summary>
        /// Gets the current value of the counter.
        /// </summary>
        public long Value => Interlocked.Read(ref this.value);

        /// <summary>
        /// Increments the counter by one.
        /// </summary>
        public void Increment()
        {
            Interlocked.Increment(ref this.value);
        }
    }
}
