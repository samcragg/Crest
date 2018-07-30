namespace Host.UnitTests.Diagnostics
{
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using Xunit;

    public class RequestMetricsTests
    {
        public sealed class GetSizes : RequestMetricsTests
        {
            [Fact]
            public void ShouldIncludeTheRequestSize()
            {
                var metrics = new RequestMetrics
                {
                    RequestSize = 123,
                };

                string result = metrics.GetSizes();

                result.Should().Contain("Request: 123");
            }

            [Fact]
            public void ShouldIncludeTheResponseSize()
            {
                var metrics = new RequestMetrics
                {
                    ResponseSize = 123,
                };

                string result = metrics.GetSizes();

                result.Should().Contain("Response: 123");
            }
        }

        public sealed class GetTimings : RequestMetricsTests
        {
            [Fact]
            public void ShouldIncludeMatchTimings()
            {
                var metrics = new RequestMetrics
                {
                    Start = 10,
                    PreRequest = 30,
                };

                string result = metrics.GetTimings();

                result.Should().Contain("Match: 20");
            }

            [Fact]
            public void ShouldIncludePostRequestTimings()
            {
                var metrics = new RequestMetrics
                {
                    PostRequest = 10,
                    WriteResponse = 30,
                };

                string result = metrics.GetTimings();

                result.Should().Contain("After: 20");
            }

            [Fact]
            public void ShouldIncludePreRequestTimings()
            {
                var metrics = new RequestMetrics
                {
                    PreRequest = 10,
                    ProcessRequest = 30,
                };

                string result = metrics.GetTimings();

                result.Should().Contain("Before: 20");
            }

            [Fact]
            public void ShouldIncludeProcessTimings()
            {
                var metrics = new RequestMetrics
                {
                    ProcessRequest = 10,
                    PostRequest = 30,
                };

                string result = metrics.GetTimings();

                result.Should().Contain("Process: 20");
            }

            [Fact]
            public void ShouldIncludeTheTotal()
            {
                var metrics = new RequestMetrics
                {
                    Start = 10,
                    Complete = 30
                };

                string result = metrics.GetTimings();

                result.Should().Contain("Total: 20");
            }

            [Fact]
            public void ShouldIncludeWriteTimings()
            {
                var metrics = new RequestMetrics
                {
                    WriteResponse = 10,
                    Complete = 30,
                };

                string result = metrics.GetTimings();

                result.Should().Contain("Write: 20");
            }
        }

        public sealed class Total : RequestMetricsTests
        {
            [Fact]
            public void ShouldReportTheDifferenceInMicroseconds()
            {
                var metrics = new RequestMetrics
                {
                    Start = 1_000,
                    Complete = 3_000,
                };

                long result = metrics.Total;

                result.Should().Be(2_000);
            }
        }
    }
}
