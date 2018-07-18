namespace Host.UnitTests
{
    using System;
    using System.Threading;
    using Crest.Host.Logging;
    using Crest.Host.Logging.LogProviders;
    using NSubstitute;

    internal static class FakeLogger
    {
        private static readonly object LockObject = new object();
        
        [ThreadStatic]
        private static LogLevel level;
        
        [ThreadStatic]
        private static string message;

        internal static void InterceptLogger()
        {
            Logger logger = (logLevel, messageFunc, exception, formatParameters) =>
            {
                level = logLevel;
                message =
                    messageFunc == null ?
                    string.Empty :
                    LogMessageFormatter.SimulateStructuredLogging(messageFunc, formatParameters)();

                return true;
            };

            Log.CurrentLogProvider = Substitute.For<ILogProvider>();
            Log.CurrentLogProvider.GetLogger(null)
                .ReturnsForAnyArgs(logger);
        }

        internal static LogInfo MonitorLogging()
        {
            var disposableLock = new LogInfo();
            level = default;
            message = null;
            return disposableLock;
        }

        internal class LogInfo : IDisposable
        {
            public LogInfo()
            {
                Monitor.Enter(LockObject);
            }

            public LogLevel LogLevel => level;

            public string Message => message;

            public void Dispose()
            {
                Monitor.Exit(LockObject);
            }
        }
    }
}
