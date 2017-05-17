namespace Host.UnitTests.Diagnostics
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using Xunit;

    public class TimeProviderTests
    {
        private readonly TimeProvider provider = new TimeProvider();

        public sealed class GetCurrentMicroseconds : TimeProviderTests
        {
            [Fact]
            public void ShouldReturnTheCurrentTimestamp()
            {
                var current = TimeSpan.FromSeconds(Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);

                long microseconds = this.provider.GetCurrentMicroseconds();

                TimeSpan.FromMilliseconds(microseconds / 1000.0).Should().BeCloseTo(current);
            }

            [Fact]
            public async Task ShouldReturnTheValueInMicroseconds()
            {
                var sw = Stopwatch.StartNew();
                long before = this.provider.GetCurrentMicroseconds();

                await Task.Delay(TimeSpan.FromMilliseconds(100));
                sw.Stop();
                long after = this.provider.GetCurrentMicroseconds();

                long delta = after - before;
                TimeSpan.FromMilliseconds(delta / 1000.0).Should().BeCloseTo(sw.Elapsed);
            }
        }
    }
}
