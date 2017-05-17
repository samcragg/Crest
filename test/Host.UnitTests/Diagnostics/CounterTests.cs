namespace Host.UnitTests.Diagnostics
{
    using Crest.Host.Diagnostics;
    using FluentAssertions;
    using Xunit;

    public class CounterTests
    {
        private Counter counter;

        public CounterTests()
        {
            this.counter = new Counter();
        }

        public sealed class Value : CounterTests
        {
            [Fact]
            public void ShouldDefaultToZero()
            {
                this.counter.Value.Should().Be(0);
            }
        }

        public sealed class Increment : CounterTests
        {
            [Fact]
            public void ShouldIncreaseTheValueByOne()
            {
                this.counter.Increment();

                this.counter.Value.Should().Be(1);
            }
        }
    }
}
