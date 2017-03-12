namespace Host.UnitTests.Conversion
{
    using System;
    using System.IO;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class BlockStreamTests
    {
        private BlockStreamPool pool;
        private BlockStream stream;

        [SetUp]
        public void SetUp()
        {
            this.pool = new BlockStreamPool();
            this.stream = new BlockStream(this.pool);
        }

        [TestFixture]
        public sealed class CanRead : BlockStreamTests
        {
            [Test]
            public void ShouldReturnTrue()
            {
                this.stream.CanRead.Should().BeTrue();
            }

            [Test]
            public void ShouldThrowIfDisposed()
            {
                this.stream.Dispose();

                this.stream.Invoking(s => { var _ = s.CanRead; })
                    .ShouldThrow<ObjectDisposedException>();
            }
        }

        [TestFixture]
        public sealed class CanSeek : BlockStreamTests
        {
            [Test]
            public void ShouldReturnTrue()
            {
                this.stream.CanSeek.Should().BeTrue();
            }

            [Test]
            public void ShouldThrowIfDisposed()
            {
                this.stream.Dispose();

                this.stream.Invoking(s => { var _ = s.CanSeek; })
                    .ShouldThrow<ObjectDisposedException>();
            }
        }

        [TestFixture]
        public sealed class CanWrite : BlockStreamTests
        {
            [Test]
            public void ShouldReturnTrue()
            {
                this.stream.CanWrite.Should().BeTrue();
            }

            [Test]
            public void ShouldThrowIfDisposed()
            {
                this.stream.Dispose();

                this.stream.Invoking(s => { var _ = s.CanWrite; })
                    .ShouldThrow<ObjectDisposedException>();
            }
        }

        [TestFixture]
        public sealed class Dispose : BlockStreamTests
        {
            [Test]
            public void ShouldReleaseTheMemoryToThePool()
            {
                // This block will be recycled
                byte[] block = this.pool.GetBlock();
                this.pool.ReturnBlocks(new[] { block });

                // Write something to force it to grab a block
                this.stream.Write(new byte[10], 0, 10);
                this.stream.Dispose();

                this.pool.GetBlock().Should().BeSameAs(block);
            }
        }

        [TestFixture]
        public sealed class Flush : BlockStreamTests
        {
            [Test]
            public void ShouldNotThrowAnException()
            {
                this.stream.Invoking(s => s.Flush())
                    .ShouldNotThrow();
            }

            [Test]
            public void ShouldThrowIfDisposed()
            {
                this.stream.Dispose();

                this.stream.Invoking(s => s.Flush())
                    .ShouldThrow<ObjectDisposedException>();
            }
        }

        [TestFixture]
        public sealed class Length : BlockStreamTests
        {
            [Test]
            public void ShouldThrowIfDisposed()
            {
                this.stream.Dispose();

                this.stream.Invoking(s => { var _ = s.Length; })
                    .ShouldThrow<ObjectDisposedException>();
            }
        }

        [TestFixture]
        public sealed class Read : BlockStreamTests
        {
            [Test]
            public void ShouldReadAllTheBytes()
            {
                byte[] data = new byte[BlockStreamPool.DefaultBlockSize + 1];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = 1;
                }
                this.stream.Write(data, 0, data.Length);
                this.stream.Position = 0;

                byte[] buffer = new byte[data.Length + 2];
                int read = this.stream.Read(buffer, 1, data.Length);

                read.Should().Be(data.Length);
                buffer[0].Should().Be(0);
                buffer[1].Should().Be(1);
                buffer[buffer.Length - 2].Should().Be(1);
                buffer[buffer.Length - 1].Should().Be(0);
            }

            [Test]
            public void ShouldReturnZeroForEmptyStreams()
            {
                byte[] buffer = new byte[1];

                int result = this.stream.Read(buffer, 0, 1);

                result.Should().Be(0);
            }

            [Test]
            public void ShouldThrowIfDisposed()
            {
                this.stream.Dispose();

                byte[] buffer = new byte[1];
                this.stream.Invoking(s => s.Read(buffer, 0, 1))
                    .ShouldThrow<ObjectDisposedException>();
            }
        }

        [TestFixture]
        public sealed class Seek : BlockStreamTests
        {
            [Test]
            public void ShouldSetThePositionFromTheCurrentPosition()
            {
                this.stream.Position = 1;

                this.stream.Seek(2, SeekOrigin.Current);

                this.stream.Position.Should().Be(3);
            }

            [Test]
            public void ShouldSetThePositionFromTheEnd()
            {
                this.stream.SetLength(1);

                this.stream.Seek(2, SeekOrigin.End);

                this.stream.Position.Should().Be(3);
            }

            [Test]
            public void ShouldSetThePositionFromTheStart()
            {
                this.stream.Position = 1;

                this.stream.Seek(2, SeekOrigin.Begin);

                this.stream.Position.Should().Be(2);
            }

            [Test]
            public void ShouldThrowIfDisposed()
            {
                this.stream.Dispose();

                this.stream.Invoking(s => s.Seek(0, SeekOrigin.Begin))
                    .ShouldThrow<ObjectDisposedException>();
            }
        }

        [TestFixture]
        public sealed class SetLength : BlockStreamTests
        {
            [Test]
            public void ShouldEnsurePositionIsSmaller()
            {
                this.stream.Position = 12;

                this.stream.SetLength(5);

                this.stream.Position.Should().Be(5);
            }

            [Test]
            public void ShouldThrowIfDisposed()
            {
                this.stream.Dispose();

                this.stream.Invoking(s => s.SetLength(0))
                    .ShouldThrow<ObjectDisposedException>();
            }

            [Test]
            public void ShouldUpdateTheLength()
            {
                this.stream.SetLength(3);

                stream.Length.Should().Be(3);
            }
        }

        [TestFixture]
        public sealed class Write : BlockStreamTests
        {
            [Test]
            public void ShouldThrowIfDisposed()
            {
                this.stream.Dispose();

                byte[] buffer = new byte[1];
                this.stream.Invoking(s => s.Write(buffer, 0, 1))
                    .ShouldThrow<ObjectDisposedException>();
            }
        }
    }
}
