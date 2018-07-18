// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Logging
{
    using System;

    /// <summary>
    /// Provides additional methods for resolving a logger for a class.
    /// </summary>
    internal static class Log
    {
        /// <summary>
        /// Gets or sets the log provider that overrides the default.
        /// </summary>
        internal static ILogProvider CurrentLogProvider { get; set; }

        /// <summary>
        /// Gets a logger for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to get the logger for.</typeparam>
        /// <returns>A logger instance.</returns>
        public static ILog For<T>()
        {
            return For(typeof(T));
        }

        /// <summary>
        /// Gets a logger for the specified type.
        /// </summary>
        /// <param name="type">The type to get the logger for.</param>
        /// <returns>A logger instance.</returns>
        public static ILog For(Type type)
        {
            ILogProvider logProvider = CurrentLogProvider ?? LogProvider.ResolveLogProvider();
            if (logProvider == null)
            {
                return NoOpLogger.Instance;
            }
            else
            {
                return new LoggerExecutionWrapper(logProvider.GetLogger(type.Name));
            }
        }

        private class NoOpLogger : ILog
        {
            internal static ILog Instance { get; } = new NoOpLogger();

            public bool Log(
                LogLevel logLevel,
                Func<string> messageFunc,
                Exception exception = null,
                params object[] formatParameters)
            {
                return false;
            }
        }
    }
}
