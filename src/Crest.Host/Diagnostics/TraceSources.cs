// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System.Diagnostics;

    /// <summary>
    /// Contains common trace sources used by the framework.
    /// </summary>
    internal static class TraceSources
    {
        /// <summary>
        /// Gets the instance for tracing routing information.
        /// </summary>
        public static TraceSource Routing { get; } = new TraceSource(nameof(Routing));

        /// <summary>
        /// Writes an error message to the specified trace source using the
        /// specified message.
        /// </summary>
        /// <param name="source">The instance to trace the event to.</param>
        /// <param name="message">The error message to write.</param>
        [Conditional("TRACE")]
        public static void TraceError(this TraceSource source, string message)
        {
            source.TraceEvent(TraceEventType.Error, 0, message, null);
        }

        /// <summary>
        /// Writes an error message to the specified trace source using the
        /// specified object array and formatting information.
        /// </summary>
        /// <param name="source">The instance to trace the event to.</param>
        /// <param name="format">
        /// A composite format string that contains text intermixed with zero
        /// or more format items, which correspond to objects in the args array.
        /// </param>
        /// <param name="args">
        /// An array containing zero or more objects to format.
        /// </param>
        [Conditional("TRACE")]
        public static void TraceError(this TraceSource source, string format, params object[] args)
        {
            source.TraceEvent(TraceEventType.Error, 0, format, args);
        }

        /// <summary>
        /// Writes a warning message to the specified trace source using the
        /// specified message.
        /// </summary>
        /// <param name="source">The instance to trace the event to.</param>
        /// <param name="message">The error message to write.</param>
        [Conditional("TRACE")]
        public static void TraceWarning(this TraceSource source, string message)
        {
            source.TraceEvent(TraceEventType.Warning, 0, message, null);
        }

        /// <summary>
        /// Writes a warning message to the specified trace source using the
        /// specified object array and formatting information.
        /// </summary>
        /// <param name="source">The instance to trace the event to.</param>
        /// <param name="format">
        /// A composite format string that contains text intermixed with zero
        /// or more format items, which correspond to objects in the args array.
        /// </param>
        /// <param name="args">
        /// An array containing zero or more objects to format.
        /// </param>
        [Conditional("TRACE")]
        public static void TraceWarning(this TraceSource source, string format, params object[] args)
        {
            source.TraceEvent(TraceEventType.Warning, 0, format, args);
        }
    }
}
