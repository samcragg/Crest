namespace Host.UnitTests.Diagnostics
{
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using Host.UnitTests.TestHelpers;
    using Xunit;

    public class TimeUnitTests
    {
        private readonly TimeUnit time = new TimeUnit();

        public sealed class Format : TimeUnitTests
        {
            [Fact]
            public void ShouldNotOutputNegativeTimes()
            {
                string result = this.time.Format(-1);

                result.Should().Be("0");
            }

            [Fact]
            public void ShouldOutputMicroSeconds()
            {
                string result = this.time.Format(20);

                result.Should().Be("20µs");
            }

            [Fact]
            public void ShouldOutputTheTimingsInMilliSeconds()
            {
                string result = this.time.Format(2_000);

                result.Should().Be("2.0ms");
            }

            [Fact]
            public void ShouldOutputTheTimingsInSeconds()
            {
                string result = this.time.Format(2_000_000);

                result.Should().Be("2.0s");
            }

            [Fact]
            [UseCulture("ES")]
            public void ShouldUseTheInvariantCulture()
            {
                string result = this.time.Format(1_200);

                result.Should().Be("1.2ms");
            }
        }

        public sealed class ValueDescription : TimeUnitTests
        {
            [Fact]
            public void ShouldReturnAHumanReadbaleValue()
            {
                string result = this.time.ValueDescription;

                result.Should().Be("microseconds");
            }
        }
    }
}
