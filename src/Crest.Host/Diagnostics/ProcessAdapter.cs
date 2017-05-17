// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Wraps around a <see cref="Process"/> to allow the values to be tested.
    /// </summary>
    internal class ProcessAdapter
    {
        private readonly Process process;
        private readonly DateTime startTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessAdapter"/> class.
        /// </summary>
        /// <param name="process">The process to wrap.</param>
        internal ProcessAdapter(Process process)
        {
            this.process = process;
            this.startTime = this.process.StartTime.ToUniversalTime();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessAdapter"/> class.
        /// </summary>
        /// <remarks>
        /// Only used to allow mock classes for unit testing.
        /// </remarks>
        protected ProcessAdapter()
        {
        }

        /// <summary>
        /// Gets the total amount of CPU time spent in applicaiton code of the
        /// process.
        /// </summary>
        public virtual TimeSpan ApplicationCpuTime => this.process.UserProcessorTime;

        /// <summary>
        /// Gets the total amount of CPU time spent in the OS for the process.
        /// </summary>
        public virtual TimeSpan SystemCpuTime => this.process.PrivilegedProcessorTime;

        /// <summary>
        /// Gets the total amount of time the process has been running for.
        /// </summary>
        public virtual TimeSpan UpTime => DateTime.UtcNow - this.startTime;

        /// <summary>
        /// Gets the amount of physical memory allocated for the process.
        /// </summary>
        public virtual long WorkingMemory => this.process.WorkingSet64;

        /// <summary>
        /// Gets the amount of the virtual memory allocated for the process.
        /// </summary>
        public virtual long VirtualMemory => this.process.VirtualMemorySize64;

        /// <summary>
        /// Clears any cached values retained by the underlying component.
        /// </summary>
        public virtual void Refresh()
        {
            this.process.Refresh();
        }
    }
}
