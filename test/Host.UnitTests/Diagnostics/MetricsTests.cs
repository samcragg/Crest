namespace Host.UnitTests.Diagnostics
{
    using System;
    using Crest.Abstractions;
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using Host.UnitTests.TestHelpers;
    using NSubstitute;
    using Xunit;

    public class MetricsTests
    {
        private readonly Metrics metrics;
        private readonly ITimeProvider time;

        private MetricsTests()
        {
            this.time = Substitute.For<ITimeProvider>();
            this.metrics = new Metrics(this.time);
        }

        private void AssertMarkMethod(Action<Metrics> method, string propertyName)
        {
            var expectedTimings = new RequestMetrics();
            typeof(RequestMetrics).GetProperty(propertyName).SetValue(expectedTimings, 123L);

            this.metrics.BeginMatch();
            this.time.GetCurrentMicroseconds().Returns(123, 0);
            method(this.metrics);

            using (FakeLogger.LogInfo log = FakeLogger.MonitorLogging())
            {
                this.metrics.EndRequest(0);
                log.Message.Should().Contain(expectedTimings.GetTimings());
            }
        }

        public sealed class BeginMatch : MetricsTests
        {
            [Fact]
            public void ShouldStartTiming()
            {
                this.metrics.BeginMatch();

                this.time.Received().GetCurrentMicroseconds();
            }
        }

        public sealed class BeginRequest : MetricsTests
        {
            [Fact]
            public void ShouldRecordTheRequestSize()
            {
                IRequestData requestData = Substitute.For<IRequestData>();
                requestData.Headers.TryGetValue("Content-Length", out Arg.Any<string>())
                    .Returns(ci =>
                    {
                        ci[1] = "123";
                        return true;
                    });

                this.metrics.BeginMatch();
                this.metrics.BeginRequest(requestData);

                // The request size isn't directly exposed, so we have to write
                // it out to a reporter to read it, however, the stats only get
                // updated at the end of a request
                IReporter reporter = Substitute.For<IReporter>();
                this.metrics.EndRequest(0);
                this.metrics.WriteTo(reporter);

                reporter.Received().Write(
                    "requestSize",
                    Arg.Is<Gauge>(g => g.Maximum == 123),
                    Arg.Any<IUnit>());
            }

            [Fact]
            public void ShouldUpdateTheCorrectProperty()
            {
                AssertMarkMethod(
                    m => m.BeginRequest(Substitute.For<IRequestData>()),
                    nameof(RequestMetrics.PreRequest));
            }
        }

        public sealed class MarkStartPostProcessing : MetricsTests
        {
            [Fact]
            public void ShouldUpdateTheCorrectProperty()
            {
                AssertMarkMethod(m => m.MarkStartPostProcessing(), nameof(RequestMetrics.PostRequest));
            }
        }

        public sealed class MarkStartPreProcessing : MetricsTests
        {
            [Fact]
            public void ShouldUpdateTheCorrectProperty()
            {
                AssertMarkMethod(m => m.MarkStartPreProcessing(), nameof(RequestMetrics.PreRequest));
            }
        }

        public sealed class MarkStartProcessing : MetricsTests
        {
            [Fact]
            public void ShouldUpdateTheCorrectProperty()
            {
                AssertMarkMethod(m => m.MarkStartProcessing(), nameof(RequestMetrics.ProcessRequest));
            }
        }

        public sealed class MarkStartWriting : MetricsTests
        {
            [Fact]
            public void ShouldUpdateTheCorrectProperty()
            {
                AssertMarkMethod(m => m.MarkStartWriting(), nameof(RequestMetrics.WriteResponse));
            }
        }

        public sealed class WriteTo : MetricsTests
        {
            private readonly IReporter reporter = Substitute.For<IReporter>();

            [Fact]
            public void ShouldWriteTheNumberOfRequests()
            {
                this.metrics.WriteTo(this.reporter);

                this.reporter.Received().Write("requestCount", Arg.Any<Counter>(), Arg.Any<IUnit>());
            }

            [Fact]
            public void ShouldWriteTheRequestSize()
            {
                this.metrics.WriteTo(this.reporter);

                this.reporter.Received().Write("requestSize", Arg.Any<Gauge>(), Arg.Any<BytesUnit>());
            }

            [Fact]
            public void ShouldWriteTheResponseSize()
            {
                this.metrics.WriteTo(this.reporter);

                this.reporter.Received().Write("responseSize", Arg.Any<Gauge>(), Arg.Any<BytesUnit>());
            }

            [Fact]
            public void ShouldWriteTheTimingOfRequests()
            {
                this.metrics.WriteTo(this.reporter);

                this.reporter.Received().Write("requestTime", Arg.Any<Gauge>(), Arg.Any<TimeUnit>());
            }
        }
    }
}
