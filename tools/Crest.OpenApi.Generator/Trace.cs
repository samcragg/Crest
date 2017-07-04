// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi.Generator
{
    using System.Diagnostics;

    /// <summary>
    /// Allows the writing of output.
    /// </summary>
    internal static class Trace
    {
        /// <summary>
        /// Gets or sets where to trace to.
        /// </summary>
        internal static TraceSource TraceSource { get; set; }

        /// <summary>
        /// Writes an error message to the trace listeners.
        /// </summary>
        /// <param name="message">The message to write.</param>
        [Conditional("TRACE")]
        public static void Error(string message)
        {
            TraceSource.TraceEvent(TraceEventType.Error, 0, message, null);
        }

        /// <summary>
        /// Writes an error message to the trace listeners.
        /// </summary>
        /// <param name="format">A composite format string to write.</param>
        /// <param name="args">
        /// An array containing zero or more objects to format.
        /// </param>
        [Conditional("TRACE")]
        public static void Error(string format, params object[] args)
        {
            TraceSource.TraceEvent(TraceEventType.Error, 0, format, args);
        }

        /// <summary>
        /// Writes an informational message to the trace listeners.
        /// </summary>
        /// <param name="message">The message to write.</param>
        [Conditional("TRACE")]
        public static void Information(string message)
        {
            TraceSource.TraceEvent(TraceEventType.Information, 0, message, null);
        }

        /// <summary>
        /// Writes an informational message to the trace listeners.
        /// </summary>
        /// <param name="format">A composite format string to write.</param>
        /// <param name="args">
        /// An array containing zero or more objects to format.
        /// </param>
        [Conditional("TRACE")]
        public static void Information(string format, params object[] args)
        {
            TraceSource.TraceEvent(TraceEventType.Information, 0, format, args);
        }

        /// <summary>
        /// Writes a verbose message to the trace listeners.
        /// </summary>
        /// <param name="message">The message to write.</param>
        [Conditional("TRACE")]
        public static void Verbose(string message)
        {
            TraceSource.TraceEvent(TraceEventType.Verbose, 0, message, null);
        }

        /// <summary>
        /// Writes a verbose message to the trace listeners.
        /// </summary>
        /// <param name="format">A composite format string to write.</param>
        /// <param name="args">
        /// An array containing zero or more objects to format.
        /// </param>
        [Conditional("TRACE")]
        public static void Verbose(string format, params object[] args)
        {
            TraceSource.TraceEvent(TraceEventType.Verbose, 0, format, args);
        }

        /// <summary>
        /// Writes a warning message to the trace listeners.
        /// </summary>
        /// <param name="message">The message to write.</param>
        [Conditional("TRACE")]
        public static void Warning(string message)
        {
            TraceSource.TraceEvent(TraceEventType.Warning, 0, message, null);
        }

        /// <summary>
        /// Writes a warning message to the trace listeners.
        /// </summary>
        /// <param name="format">A composite format string to write.</param>
        /// <param name="args">
        /// An array containing zero or more objects to format.
        /// </param>
        [Conditional("TRACE")]
        public static void Warning(string format, params object[] args)
        {
            TraceSource.TraceEvent(TraceEventType.Warning, 0, format, args);
        }
    }
}
