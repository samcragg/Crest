// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi.Generator
{
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Allows the writing of output.
    /// </summary>
    internal static class Trace
    {
        private static TraceSource traceSource;

        /// <summary>
        /// Writes an error message to the trace listeners.
        /// </summary>
        /// <param name="message">The message to write.</param>
        [Conditional("TRACE")]
        public static void Error(string message)
        {
            traceSource.TraceEvent(TraceEventType.Error, 0, message, null);
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
            traceSource.TraceEvent(TraceEventType.Error, 0, format, args);
        }

        /// <summary>
        /// Writes an informational message to the trace listeners.
        /// </summary>
        /// <param name="message">The message to write.</param>
        [Conditional("TRACE")]
        public static void Information(string message)
        {
            traceSource.TraceEvent(TraceEventType.Information, 0, message, null);
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
            traceSource.TraceEvent(TraceEventType.Information, 0, format, args);
        }

        /// <summary>
        /// Writes a verbose message to the trace listeners.
        /// </summary>
        /// <param name="message">The message to write.</param>
        [Conditional("TRACE")]
        public static void Verbose(string message)
        {
            traceSource.TraceEvent(TraceEventType.Verbose, 0, message, null);
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
            traceSource.TraceEvent(TraceEventType.Verbose, 0, format, args);
        }

        /// <summary>
        /// Writes a warning message to the trace listeners.
        /// </summary>
        /// <param name="message">The message to write.</param>
        [Conditional("TRACE")]
        public static void Warning(string message)
        {
            traceSource.TraceEvent(TraceEventType.Warning, 0, message, null);
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
            traceSource.TraceEvent(TraceEventType.Warning, 0, format, args);
        }

        /// <summary>
        /// Sets up the tracing level.
        /// </summary>
        /// <param name="level">The level at which to trace.</param>
        [Conditional("TRACE")]
        internal static void SetUpTrace(string level)
        {
            traceSource = new TraceSource("Crest.OpenApi.Generator");
            traceSource.Listeners.Add(new ConsoleTraceListener());
            traceSource.Switch.Level = GetTraceLevel(level ?? string.Empty);
        }

        private static SourceLevels GetTraceLevel(string level)
        {
            switch (level.ToLowerInvariant().FirstOrDefault())
            {
                case 'q':
                    return SourceLevels.Error;

                case 'm':
                    return SourceLevels.Warning;

                case 'n':
                    return SourceLevels.Information;

                case 'd':
                    return SourceLevels.Verbose;
            }

            return SourceLevels.Warning;
        }
    }
}
