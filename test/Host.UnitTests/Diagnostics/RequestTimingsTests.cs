namespace Host.UnitTests.Diagnostics
{
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using Xunit;

    public class RequestTimingsTests
    {
        public new sealed class ToString : RequestTimingsTests
        {
            [Fact]
            public void ShouldIncludeMatchTimings()
            {
                var timings = new RequestTimings
                {
                    Start = 10,
                    PreRequest = 30,
                };

                string result = timings.ToString();

                result.Should().Contain("Match: 20");
            }

            [Fact]
            public void ShouldIncludePostRequestTimings()
            {
                var timings = new RequestTimings
                {
                    PostRequest = 10,
                    WriteResponse = 30,
                };

                string result = timings.ToString();

                result.Should().Contain("After: 20");
            }

            [Fact]
            public void ShouldIncludePreRequestTimings()
            {
                var timings = new RequestTimings
                {
                    PreRequest = 10,
                    ProcessRequest = 30,
                };

                string result = timings.ToString();

                result.Should().Contain("Before: 20");
            }

            [Fact]
            public void ShouldIncludeProcessTimings()
            {
                var timings = new RequestTimings
                {
                    ProcessRequest = 10,
                    PostRequest = 30,
                };

                string result = timings.ToString();

                result.Should().Contain("Process: 20");
            }

            [Fact]
            public void ShouldIncludeWriteTimings()
            {
                var timings = new RequestTimings
                {
                    WriteResponse = 10,
                    Complete = 30,
                };

                string result = timings.ToString();

                result.Should().Contain("Write: 20");
            }

            [Fact]
            public void ShouldOutputTheTimingsInMicroSeconds()
            {
                var timings = new RequestTimings
                {
                    Complete = 20
                };

                string result = timings.ToString();

                result.Should().EndWith("Total: 20µs");
            }

            [Fact]
            public void ShouldOutputTheTimingsInMilliSeconds()
            {
                var timings = new RequestTimings
                {
                    Complete = 2_000
                };

                string result = timings.ToString();

                result.Should().EndWith("Total: 2.0ms");
            }

            [Fact]
            public void ShouldOutputTheTimingsInSeconds()
            {
                var timings = new RequestTimings
                {
                    Complete = 2_000_000
                };

                string result = timings.ToString();

                result.Should().EndWith("Total: 2.0s");
            }

            [Fact]
            public void ShouldOutputZeroIfTheTimingIsUnknown()
            {
                var timings = new RequestTimings
                {
                    Start = 1
                };

                string result = timings.ToString();

                result.Should().EndWith("Total: 0");
            }
        }

        public sealed class TotalMs : RequestTimingsTests
        {
            [Fact]
            public void ShouldConvertTheTimestampsToMilliseconds()
            {
                var timings = new RequestTimings
                {
                    Complete = 2_000
                };

                double result = timings.TotalMs;

                result.Should().BeApproximately(2, 0.001);
            }
        }
    }
}
