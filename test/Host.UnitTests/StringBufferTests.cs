namespace Host.UnitTests
{
    using System;
    using Crest.Host;
    using FluentAssertions;
    using Host.UnitTests.TestHelpers;
    using Xunit;

    public class StringBufferTests
    {
        private const int LengthToForceMultipleBuffers = 5000;
        private readonly StringBuffer buffer = new StringBuffer();

        private void AppendMultiple(char c, int count, StringBuffer stringBuffer = null)
        {
            stringBuffer = stringBuffer ?? this.buffer;
            for (int i = 0; i < count; i++)
            {
                stringBuffer.Append(c);
            }
        }

        public sealed class AppendAscii : StringBufferTests
        {
            [Fact]
            public void ShouldAcceptEmptyValues()
            {
                this.buffer.AppendAscii(new byte[0]);

                string result = this.buffer.ToString();

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldAddLargeBuffers()
            {
                Span<byte> bytes = new byte[LengthToForceMultipleBuffers];
                bytes.Fill((byte)'X');
                this.buffer.Append('1');
                this.buffer.AppendAscii(bytes);

                string result = this.buffer.ToString();

                result.Should().StartWith("1X");
                result.Should().HaveLength(LengthToForceMultipleBuffers + 1);
            }

            [Fact]
            public void ShouldAddTheValueToTheEnd()
            {
                this.buffer.Append('1');
                this.buffer.AppendAscii(new byte[] { (byte)'2' });

                string result = this.buffer.ToString();

                result.Should().Be("12");
            }
        }

        public sealed class AppendChar : StringBufferTests
        {
            [Fact]
            public void ShouldAddTheValueToTheEnd()
            {
                this.buffer.Append('1');
                this.buffer.Append('2');

                string result = this.buffer.ToString();

                result.Should().Be("12");
            }
        }

        public sealed class AppendString : StringBufferTests
        {
            [Fact]
            public void ShouldAcceptNullValues()
            {
                this.buffer.Append('1');
                this.buffer.Append(null);

                string result = this.buffer.ToString();

                result.Should().Be("1");
            }

            [Fact]
            public void ShouldAddLargeStrings()
            {
                this.buffer.Append('1');
                this.buffer.Append(new string('X', LengthToForceMultipleBuffers));

                string result = this.buffer.ToString();

                result.Should().StartWith("1X");
                result.Should().HaveLength(LengthToForceMultipleBuffers + 1);
            }

            [Fact]
            public void ShouldAddTheValueToTheEnd()
            {
                this.buffer.Append('1');
                this.buffer.Append("2");

                string result = this.buffer.ToString();

                result.Should().Be("12");
            }
        }

        public sealed class Clear : StringBufferTests
        {
            [Fact]
            public void ShouldIgnoreWhatWasWritten()
            {
                this.AppendMultiple('a', 10);

                this.buffer.Clear();
                this.AppendMultiple('b', 10);
                string result = this.buffer.ToString();

                result.Should().Be(new string('b', 10));
            }

            [Fact]
            public void ShouldReleaseLargeBuffers()
            {
                lock (FakeArrayPool.LockObject)
                {
                    FakeArrayPool<char>.Instance.Reset();
                    this.AppendMultiple(' ', LengthToForceMultipleBuffers);

                    int inUseAllocated = FakeArrayPool<char>.Instance.TotalAllocated;
                    this.buffer.Clear();

                    FakeArrayPool<char>.Instance.TotalAllocated
                        .Should().BeLessThan(inUseAllocated);
                }
            }
        }

        public sealed class CreateSpan : StringBufferTests
        {
            [Fact]
            public void ShouldReturnAnEmptySpanIfNothingHasBeenAdded()
            {
                ReadOnlySpan<char> span = this.buffer.CreateSpan();

                span.IsEmpty.Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnASingleSpanForLargeBuffers()
            {
                this.AppendMultiple(' ', LengthToForceMultipleBuffers);

                ReadOnlySpan<char> span = this.buffer.CreateSpan();

                span.Length.Should().Be(LengthToForceMultipleBuffers);
            }

            [Fact]
            public void ShouldReturnASpanContaingOnlyTheUsedData()
            {
                this.buffer.Append('a');
                this.buffer.Append('b');
                this.buffer.Append('c');
                this.buffer.TrimEnds(c => c == 'a');

                ReadOnlySpan<char> span = this.buffer.CreateSpan();

                span.Length.Should().Be(2);
                span[0].Should().Be('b');
                span[1].Should().Be('c');
            }
        }

        public sealed class Dispose : StringBufferTests
        {
            [Fact]
            public void ShouldReturnAllBuffers()
            {
                lock (FakeArrayPool.LockObject)
                {
                    FakeArrayPool<char>.Instance.Reset();

                    // Create the buffer after we've got exclusive access to the
                    // array pool
                    var stringBuffer = new StringBuffer();
                    this.AppendMultiple(' ', LengthToForceMultipleBuffers, stringBuffer);

                    stringBuffer.Dispose();

                    FakeArrayPool<char>.Instance.TotalAllocated.Should().Be(0);
                }
            }
        }

        public sealed class Length : StringBufferTests
        {
            [Fact]
            public void ShouldReturnTheTotalAmountAdded()
            {
                this.AppendMultiple('X', 123);

                int result = this.buffer.Length;

                result.Should().Be(123);
            }
        }

        public sealed new class ToString : StringBufferTests
        {
            [Fact]
            public void ShouldReturnAnEmptyStringIfNothingHasBeenAppended()
            {
                string result = this.buffer.ToString();

                result.Should().BeEmpty();
            }

            [Fact]
            public void ShouldReturnAStringForMultipleBuffers()
            {
                this.AppendMultiple('X', LengthToForceMultipleBuffers);

                string result = this.buffer.ToString();

                result.Should().Be(new string('X', LengthToForceMultipleBuffers));
            }
        }

        public sealed class TrimEnds : StringBufferTests
        {
            [Fact]
            public void ShouldRemoveCharactersAtTheEnd()
            {
                this.AppendMultiple('X', 2);
                this.AppendMultiple('Y', LengthToForceMultipleBuffers);

                this.buffer.TrimEnds(c => c == 'Y');
                string result = this.buffer.ToString();

                result.Should().Be("XX");
            }

            [Fact]
            public void ShouldRemoveCharactersAtTheStart()
            {
                this.AppendMultiple('X', LengthToForceMultipleBuffers);
                this.AppendMultiple('Y', 2);

                this.buffer.TrimEnds(c => c == 'X');
                string result = this.buffer.ToString();

                result.Should().Be("YY");
            }
        }

        public sealed class Truncate : StringBufferTests
        {
            [Fact]
            public void ShouldRemoveAllTheCharacters()
            {
                this.AppendMultiple('X', 2);
                this.AppendMultiple('Y', LengthToForceMultipleBuffers);

                this.buffer.Truncate(LengthToForceMultipleBuffers);
                string result = this.buffer.ToString();

                result.Should().Be("XX");
            }
        }
    }
}
