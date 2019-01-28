namespace Host.UnitTests.Serialization
{
    using Crest.Host.Serialization;
    using FluentAssertions;
    using Xunit;

    public class ArrayBufferTests
    {
        public sealed class Add
        {
            [Fact]
            public void ShouldAddTheItemToTheReturnedArray()
            {
                var buffer = new ArrayBuffer<int>();

                buffer.Add(123);
                int[] result = buffer.ToArray();

                result.Should().Equal(123);
            }

            [Theory]
            [InlineData(4)]
            [InlineData(5)]
            public void ShouldEnsureThereIsEnoughCapacityToAddAnItem(int count)
            {
                var buffer = new ArrayBuffer<int>();
                for (int i = 0; i < count; i++)
                {
                    buffer.Add(i);
                }

                int[] result = buffer.ToArray();

                result.Should().HaveCount(count);
            }
        }

        public sealed class ToArray
        {
            [Fact]
            public void ShouldReturnAnEmptyArrayIfNothingHasBeenAdded()
            {
                var buffer = new ArrayBuffer<int>();

                int[] result = buffer.ToArray();

                result.Should().NotBeNull().And.BeEmpty();
            }
        }
    }
}
