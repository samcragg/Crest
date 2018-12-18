namespace Host.UnitTests.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Crest.Host.IO;
    using FluentAssertions;
    using Host.UnitTests.TestHelpers;
    using NSubstitute;
    using Xunit;

    public class StreamIteratorTests
    {
        private readonly Lazy<StreamIterator> iterator;
        private readonly Stream stream = Substitute.For<Stream>();

        private StreamIteratorTests()
        {
            this.iterator = new Lazy<StreamIterator>(() => new StreamIterator(this.stream));
        }

        private StreamIterator Iterator => this.iterator.Value;

        private void SetStreamRead(params IEnumerable<byte>[] read)
        {
            byte[][] bytes = new byte[read.Length][];
            for (int i = 0; i < read.Length; i++)
            {
                bytes[i] = read[i].ToArray();
            }

            int index = 0;
            this.stream.Read(null, 0, 0).ReturnsForAnyArgs(ci =>
            {
                if (index < bytes.Length)
                {
                    int length = Math.Min(bytes[index].Length, ci.ArgAt<int>(2));
                    Array.Copy(bytes[index], 0, ci.ArgAt<byte[]>(0), ci.ArgAt<int>(1), length);
                    index++;
                    return length;
                }
                else
                {
                    return 0;
                }
            });
        }

        public sealed class Current : StreamIteratorTests
        {
            private const char Snowman = '☃';

            [Fact]
            public void ShouldAssumeUtf8IfNoBom()
            {
                byte[] bytes = Encoding.UTF8.GetBytes(new[] { Snowman });
                this.SetStreamRead(bytes);

                this.Iterator.MoveNext().Should().BeTrue();
                this.Iterator.Current.Should().Be(Snowman);
            }

            [Theory]
            [InlineData("utf-8")]
            [InlineData("utf-16")]
            [InlineData("unicodeFFFE")] // UTF-16 big endian
            [InlineData("utf-32")]
            [InlineData("utf-32BE")]
            public void ShouldHandleUnicodeEncodedValues(string encodingName)
            {
                // BOM + something
                var encoding = Encoding.GetEncoding(encodingName);
                this.SetStreamRead(
                    encoding.GetPreamble().Concat(encoding.GetBytes(new[] { Snowman })));

                this.Iterator.MoveNext().Should().BeTrue();
                this.Iterator.Current.Should().Be(Snowman);
            }

            [Fact]
            public void ShouldReturnDefaultIfMoveNextHasNotBeenCalled()
            {
                var iterator = new StreamIterator(Stream.Null);

                iterator.Current.Should().Be(default);
            }

            [Fact]
            public void ShouldReturnDefaultValueAtTheEndOfTheStream()
            {
                var iterator = new StreamIterator(Stream.Null);

                iterator.MoveNext();
                iterator.Current.Should().Be(default);
            }
        }

        public sealed class Dispose : StreamIteratorTests
        {
            [Fact]
            public void ShouldDisposeTheStream()
            {
                ((IDisposable)this.stream).DidNotReceive().Dispose();

                this.Iterator.Dispose();

                ((IDisposable)this.stream).Received().Dispose();
            }

            [Fact]
            public void ShouldReturnTheArraysToThePoolsOnce()
            {
                lock (FakeArrayPool.LockObject)
                {
                    FakeArrayPool<byte>.Instance.Reset();
                    FakeArrayPool<char>.Instance.Reset();

                    // Verify it's allocated something
                    this.Iterator.GetSpan(0);
                    FakeArrayPool<byte>.Instance.TotalAllocated.Should().BePositive();
                    FakeArrayPool<char>.Instance.TotalAllocated.Should().BePositive();

                    // Dispose multiple times
                    this.Iterator.Dispose();
                    this.Iterator.Dispose();

                    // We need to make sure it doesn't return the buffer twice,
                    // however, we're not concerned if the stream is disposed of
                    // multiple times, as it's a big boy and can handle it
                    FakeArrayPool<byte>.Instance.TotalAllocated.Should().Be(0);
                    FakeArrayPool<char>.Instance.TotalAllocated.Should().Be(0);
                }
            }
        }

        public sealed class GetSpan : StreamIteratorTests
        {
            [Fact]
            public void ShouldFillTheBufferUntilEngoughDataIsAvailable()
            {
                this.SetStreamRead(new[] { (byte)'a', (byte)'b' }, new[] { (byte)'c' });

                this.Iterator.MoveNext();
                ReadOnlySpan<char> span = this.Iterator.GetSpan(3);

                span.Length.Should().Be(3);
                span[2].Should().Be('c');
            }

            [Fact]
            public void ShouldReturnAnEmptySpanIfTheEndOfTheStream()
            {
                this.Iterator.MoveNext();
                ReadOnlySpan<char> span = this.Iterator.GetSpan(1);

                span.IsEmpty.Should().BeTrue();
            }

            [Fact]
            public void ShouldReturnASpanWithAllDataIfLessThanMaximum()
            {
                this.SetStreamRead(new[] { (byte)'a', (byte)'b' });

                this.Iterator.MoveNext();
                ReadOnlySpan<char> span = this.Iterator.GetSpan(3);

                span.Length.Should().Be(2);
                span[0].Should().Be('a');
                span[1].Should().Be('b');
            }

            [Fact]
            public void ShouldReturnUnreadValuesFromTheCurrentBufferWhenReadingMore()
            {
                this.SetStreamRead(new[] { (byte)'a', (byte)'b' }, new[] { (byte)'c', (byte)'d' });

                this.Iterator.MoveNext();
                this.Iterator.MoveNext();
                ReadOnlySpan<char> span = this.Iterator.GetSpan(2);

                span.Length.Should().Be(2);
                span[0].Should().Be('b');
                span[1].Should().Be('c');
            }

            [Fact]
            public void ShouldStartFromTheCurrentPosition()
            {
                this.SetStreamRead(new[] { (byte)'a', (byte)'b' });

                this.Iterator.MoveNext(); // a
                this.Iterator.MoveNext(); // b
                ReadOnlySpan<char> span = this.Iterator.GetSpan(1);

                span.Length.Should().Be(1);
                span[0].Should().Be('b');
            }
        }

        public sealed class MoveNext : StreamIteratorTests
        {
            [Fact]
            public void ShouldReadMultipleTimesFromTheStream()
            {
                this.SetStreamRead(new[] { (byte)'a' }, new[] { (byte)'b' });

                this.Iterator.MoveNext().Should().BeTrue();
                this.Iterator.Current.Should().Be('a');

                this.Iterator.MoveNext().Should().BeTrue();
                this.Iterator.Current.Should().Be('b');
            }

            [Fact]
            public void ShouldReturnFalseAtTheEndOfTheStream()
            {
                this.Iterator.MoveNext().Should().BeFalse();
                this.Iterator.MoveNext().Should().BeFalse();
            }

            [Fact]
            public void ShouldReturnTrueIfACharacterWasRead()
            {
                this.SetStreamRead(new[] { (byte)'a' });

                this.Iterator.MoveNext().Should().BeTrue();
            }
        }

        public sealed class Position : StreamIteratorTests
        {
            [Fact]
            public void ShouldDefaultToZero()
            {
                this.Iterator.Position.Should().Be(0);
            }

            [Fact]
            public void ShouldReturnTheNumberOfCharactersRead()
            {
                this.SetStreamRead(new[] { (byte)'a' });

                this.Iterator.MoveNext();

                this.Iterator.Position.Should().Be(1);
            }

            [Fact]
            public void ShouldReturnThePositionRelativeToTheStartAfterMultipleReads()
            {
                this.SetStreamRead(new[] { (byte)'a' }, new[] { (byte)'b' });

                this.Iterator.MoveNext();
                this.Iterator.MoveNext();

                this.Iterator.Position.Should().Be(2);
            }
        }

        public sealed class Skip : StreamIteratorTests
        {
            [Fact]
            public void ShouldIgnoreSpecifiedNumberOfCharacters()
            {
                this.SetStreamRead(Encoding.UTF8.GetBytes("12345"));

                this.Iterator.MoveNext();
                this.Iterator.Skip(4);

                this.Iterator.Current.Should().Be('5');
            }
        }
    }
}
