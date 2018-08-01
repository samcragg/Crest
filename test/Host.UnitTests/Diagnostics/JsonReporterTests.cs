namespace Host.UnitTests.Diagnostics
{
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using Host.UnitTests.TestHelpers;
    using Newtonsoft.Json;
    using NSubstitute;
    using Xunit;

    public class JsonReporterTests
    {
        private readonly JsonReporter reporter = new JsonReporter();

        private dynamic GetJson()
        {
            string report = this.reporter.GenerateReport();
            return JsonConvert.DeserializeObject(report);
        }

        public sealed class Dispose : JsonReporterTests
        {
            [Fact]
            public void ShouldReturnTheStringBuffer()
            {
                lock (FakeArrayPool.LockObject)
                {
                    FakeArrayPool<char> pool = FakeArrayPool<char>.Instance;
                    pool.Reset();

                    var jsonReporter = new JsonReporter();
                    jsonReporter.Write("label", new Counter(), null);
                    pool.TotalAllocated.Should().BeGreaterThan(0);

                    jsonReporter.Dispose();
                    pool.TotalAllocated.Should().Be(0);
                }
            }
        }

        public sealed class Write_Counter : JsonReporterTests
        {
            [Fact]
            public void ShouldIncludeTheUnit()
            {
                IUnit unit = Substitute.For<IUnit>();
                unit.ValueDescription.Returns("unit description");

                this.reporter.Write("counter", new Counter(), unit);
                dynamic result = this.GetJson().counter;

                ((string)result.unit).Should().Be("unit description");
            }

            [Fact]
            public void ShouldIncludeTheValue()
            {
                var counter = new Counter();
                counter.Increment();

                this.reporter.Write("counter", counter, null);
                dynamic result = this.GetJson().counter;

                ((int)result.value).Should().Be(1);
            }
        }

        public sealed class Write_Gauage : JsonReporterTests
        {
            private readonly Gauge gauge;

            public Write_Gauage()
            {
                this.gauge = new Gauge(Substitute.For<ITimeProvider>());
            }

            [Fact]
            public void ShouldIncludeTheAverages()
            {
                this.gauge.Add(50);
                this.gauge.Add(75);
                this.gauge.Add(100);

                this.reporter.Write("gauge", this.gauge, null);
                dynamic result = this.GetJson().gauge;

                ((double)result.mean).Should().BeApproximately(75, 0.1);
                ((double)result.stdDev).Should().BeApproximately(25, 0.1);
            }

            [Fact]
            public void ShouldIncludeTheBasicStats()
            {
                this.gauge.Add(50);
                this.gauge.Add(100);

                this.reporter.Write("gauge", this.gauge, null);
                dynamic result = this.GetJson().gauge;

                ((int)result.count).Should().Be(2);
                ((int)result.min).Should().Be(50);
                ((int)result.max).Should().Be(100);
            }

            [Fact]
            public void ShouldIncludeTheMovingAverages()
            {
                this.gauge.Add(100);

                this.reporter.Write("gauge", this.gauge, null);
                dynamic result = this.GetJson().gauge;

                ((int)result.movingAv1min).Should().Be(100);
                ((int)result.movingAv5min).Should().Be(100);
                ((int)result.movingAv15min).Should().Be(100);
            }

            [Fact]
            public void ShouldIncludeTheUnit()
            {
                IUnit unit = Substitute.For<IUnit>();
                unit.ValueDescription.Returns("unit description");

                this.reporter.Write("gauge", this.gauge, unit);
                dynamic result = this.GetJson().gauge;

                ((string)result.unit).Should().Be("unit description");
            }
        }
    }
}
