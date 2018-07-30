namespace Host.UnitTests.Diagnostics
{
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using NSubstitute;
    using Xunit;

    public class GaugeTests
    {
        private readonly Gauge statistics;
        private readonly ITimeProvider time;

        public GaugeTests()
        {
            this.time = Substitute.For<ITimeProvider>();
            this.statistics = new Gauge(this.time);

            long microseconds = 0;
            this.time.GetCurrentMicroseconds()
                .Returns(_ => microseconds += (1000 * 1000 * 60));
        }

        public sealed class FifteenMinuteAverage : GaugeTests
        {
            [Fact]
            public void ShouldBeZeroIfNothingHasBeenAdded()
            {
                this.statistics.FifteenMinuteAverage.Should().Be(0);
            }

            [Fact]
            public void ShouldMatchTheValueIfASingleItemHasBeenAdded()
            {
                this.statistics.Add(123);
                this.statistics.FifteenMinuteAverage.Should().BeApproximately(123, 0.01);
            }
        }

        public sealed class FiveMinuteAverage : GaugeTests
        {
            [Fact]
            public void ShouldBeZeroIfNothingHasBeenAdded()
            {
                this.statistics.FiveMinuteAverage.Should().Be(0);
            }

            [Fact]
            public void ShouldMatchTheValueIfASingleItemHasBeenAdded()
            {
                this.statistics.Add(123);
                this.statistics.FiveMinuteAverage.Should().BeApproximately(123, 0.01);
            }
        }

        public sealed class KnownDataTest : GaugeTests
        {
            [Fact]
            public void ShouldCalculateTheMetricsOfKnownData()
            {
                // These were initially chosen randomly and then their values
                // calculated in Excel
                int[] values = new[] { 61, 32, 35, 16, 27, 30, 25, 17, 36, 72, 60, 77, 70, 82, 73, 27, 24, 16, 28, 19, 24, 31, 31, 33, 27, 23, 20, 38, 26, 31 };
                foreach (int value in values)
                {
                    this.statistics.Add(value);
                }

                this.statistics.FifteenMinuteAverage.Should().BeApproximately(37.56, 0.005);
                this.statistics.FiveMinuteAverage.Should().BeApproximately(29.77, 0.005);
                this.statistics.Mean.Should().BeApproximately(37.03, 0.005);
                this.statistics.OneMinuteAverage.Should().BeApproximately(29.98, 0.005);
                this.statistics.SampleSize.Should().Be(values.Length);
                this.statistics.StandardDeviation.Should().BeApproximately(20.02, 0.005);
                this.statistics.Variance.Should().BeApproximately(400.65, 0.005);
            }
        }

        public sealed class Maximum : GaugeTests
        {
            [Fact]
            public void ShouldDefaultToZero()
            {
                this.statistics.Maximum.Should().Be(0);
            }

            [Theory]
            [InlineData(1, 1)]
            [InlineData(3, 1, 3)]
            [InlineData(4, 4, 1, 2)]
            public void ShouldReturnTheMeanOfTheValues(long expected, params int[] values)
            {
                foreach (int value in values)
                {
                    this.statistics.Add(value);
                }

                this.statistics.Maximum.Should().Be(expected);
            }
        }

        public sealed class Mean : GaugeTests
        {
            [Fact]
            public void ShouldDefaultToZero()
            {
                this.statistics.Mean.Should().Be(0);
            }

            [Theory]
            [InlineData(1.0, 1)]
            [InlineData(2.0, 1, 3)]
            [InlineData(4.0, 2, 4, 6)]
            [InlineData(4.0, 2, 3, 5, 6)]
            public void ShouldReturnTheMeanOfTheValues(double expected, params int[] values)
            {
                foreach (int value in values)
                {
                    this.statistics.Add(value);
                }

                this.statistics.Mean.Should().BeApproximately(expected, 0.01);
            }
        }

        public sealed class Minimum : GaugeTests
        {
            [Fact]
            public void ShouldDefaultToZero()
            {
                this.statistics.Minimum.Should().Be(0);
            }

            [Theory]
            [InlineData(1, 1)]
            [InlineData(1, 3, 1)]
            [InlineData(2, 2, 5, 3)]
            public void ShouldReturnTheMeanOfTheValues(long expected, params int[] values)
            {
                foreach (int value in values)
                {
                    this.statistics.Add(value);
                }

                this.statistics.Minimum.Should().Be(expected);
            }
        }

        public sealed class OneMinuteAverage : GaugeTests
        {
            [Fact]
            public void ShouldBeZeroIfNothingHasBeenAdded()
            {
                this.statistics.OneMinuteAverage.Should().Be(0);
            }

            [Fact]
            public void ShouldMatchTheValueIfASingleItemHasBeenAdded()
            {
                this.statistics.Add(123);
                this.statistics.OneMinuteAverage.Should().BeApproximately(123, 0.01);
            }
        }

        public sealed class SampleSize : GaugeTests
        {
            [Fact]
            public void ShouldBeZeroIfNothingHasBeenAdded()
            {
                this.statistics.SampleSize.Should().Be(0);
            }

            [Fact]
            public void ShouldReturnTheNumberOfAddedValues()
            {
                this.statistics.Add(0);
                this.statistics.Add(0);
                this.statistics.Add(0);

                this.statistics.SampleSize.Should().Be(3);
            }
        }

        public sealed class StandardDeviation : GaugeTests
        {
            [Fact]
            public void ShouldBeZeroIfASingleItemHasBeenAdded()
            {
                this.statistics.Add(123);
                this.statistics.StandardDeviation.Should().Be(0);
            }

            [Fact]
            public void ShouldBeZeroIfNothingHasBeenAdded()
            {
                this.statistics.StandardDeviation.Should().Be(0);
            }

            [Theory]
            [InlineData(1.0, 1, 2, 3)]
            [InlineData(2.0, 1, 3, 5)]
            public void ShouldReturnTheStandardDeviationOfTheValues(double expected, params int[] values)
            {
                foreach (int value in values)
                {
                    this.statistics.Add(value);
                }

                this.statistics.StandardDeviation.Should().BeApproximately(expected, 0.01);
            }
        }

        public sealed class Variance : GaugeTests
        {
            [Fact]
            public void ShouldBeZeroIfASingleItemHasBeenAdded()
            {
                this.statistics.Add(123);
                this.statistics.Variance.Should().Be(0);
            }

            [Fact]
            public void ShouldBeZeroIfNothingHasBeenAdded()
            {
                this.statistics.Variance.Should().Be(0);
            }

            [Theory]
            [InlineData(1.0, 1, 2, 3)]
            [InlineData(4.0, 2, 4, 6)]
            public void ShouldReturnTheStandardDeviationOfTheValues(double expected, params int[] values)
            {
                foreach (int value in values)
                {
                    this.statistics.Add(value);
                }

                this.statistics.Variance.Should().BeApproximately(expected, 0.01);
            }
        }
    }
}
