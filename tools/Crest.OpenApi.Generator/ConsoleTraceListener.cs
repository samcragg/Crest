// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi.Generator
{
    using System;
    using System.Diagnostics;
    using System.Globalization;

    /// <summary>
    /// Outputs the trace to the console.
    /// </summary>
    internal class ConsoleTraceListener : TraceListener
    {
        /// <inheritdoc />
        public override void Write(string message)
        {
            Console.Write(message);
        }

        /// <inheritdoc />
        public override void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        /// <inheritdoc />
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if ((this.Filter == null) || this.Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
            {
                this.WriteLine(message);
            }
        }

        /// <inheritdoc />
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if ((this.Filter == null) || this.Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
            {
                if (args != null)
                {
                    this.WriteLine(string.Format(CultureInfo.InvariantCulture, format, args));
                }
                else
                {
                    this.WriteLine(format);
                }
            }
        }
    }
}
