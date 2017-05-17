namespace Host.UnitTests.Diagnostics
{
    using System.Diagnostics;
    using Crest.Host.Diagnostics;
    using NSubstitute;
    using Xunit;

    public class TraceSourcesTests
    {
        private readonly TraceListener listener;
        private readonly TraceSource traceSource;

        public TraceSourcesTests()
        {
            this.listener = Substitute.For<TraceListener>();
            this.traceSource = new TraceSource("TestTraceSource", SourceLevels.All);
            this.traceSource.Listeners.Add(this.listener);
        }

        public sealed class TraceError : TraceSourcesTests
        {
            [Fact]
            public void ShouldOutputAnErrorEventForFormattedMessages()
            {
                object[] parameters = new object[0];
                this.traceSource.TraceError("format", parameters);

                this.listener.Received().TraceEvent(
                    Arg.Any<TraceEventCache>(),
                    Arg.Any<string>(),
                    TraceEventType.Error,
                    Arg.Any<int>(),
                    "format",
                    parameters);
            }

            [Fact]
            public void ShouldOutputAnErrorEventForMessages()
            {
                this.traceSource.TraceError("message");

                this.listener.Received().TraceEvent(
                    Arg.Any<TraceEventCache>(),
                    Arg.Any<string>(),
                    TraceEventType.Error,
                    Arg.Any<int>(),
                    "message",
                    Arg.Any<object[]>());
            }
        }

        public sealed class TraceWarning : TraceSourcesTests
        {
            [Fact]
            public void ShouldOutputAWarningEventForFormattedMessages()
            {
                object[] parameters = new object[0];
                this.traceSource.TraceWarning("format", parameters);

                this.listener.Received().TraceEvent(
                    Arg.Any<TraceEventCache>(),
                    Arg.Any<string>(),
                    TraceEventType.Warning,
                    Arg.Any<int>(),
                    "format",
                    parameters);
            }

            [Fact]
            public void ShouldOutputAWarningEventForMessages()
            {
                this.traceSource.TraceWarning("message");

                this.listener.Received().TraceEvent(
                    Arg.Any<TraceEventCache>(),
                    Arg.Any<string>(),
                    TraceEventType.Warning,
                    Arg.Any<int>(),
                    "message",
                    Arg.Any<object[]>());
            }
        }
    }
}
