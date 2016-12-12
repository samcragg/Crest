namespace Host.UnitTests.Conversion
{
    using System;
    using System.IO;
    using Crest.Host.Conversion;
    using NUnit.Framework;

    [TestFixture]
    public sealed class BlockStreamTests
    {
        private BlockStreamPool pool;
        private BlockStream stream;

        [SetUp]
        public void SetUp()
        {
            this.pool = new BlockStreamPool();
            this.stream = new BlockStream(this.pool);
        }

        [Test]
        public void CanReadShouldReturnTrue()
        {
            Assert.That(this.stream.CanRead, Is.True);
        }

        [Test]
        public void CanReadShouldThrowIfDisposed()
        {
            this.stream.Dispose();

            Assert.That(() => this.stream.CanRead, Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void CanSeekShouldReturnTrue()
        {
            Assert.That(this.stream.CanSeek, Is.True);
        }

        [Test]
        public void CanSeekShouldThrowIfDisposed()
        {
            this.stream.Dispose();

            Assert.That(() => this.stream.CanSeek, Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void CanWriteShouldReturnTrue()
        {
            Assert.That(this.stream.CanWrite, Is.True);
        }

        [Test]
        public void CanWriteShouldThrowIfDisposed()
        {
            this.stream.Dispose();

            Assert.That(() => this.stream.CanWrite, Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void LengthShouldThrowIfDisposed()
        {
            this.stream.Dispose();

            Assert.That(() => this.stream.Length, Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void DisposeShouldReleaseTheMemoryToThePool()
        {
            // This block will be recycled
            byte[] block = this.pool.GetBlock();
            this.pool.ReturnBlocks(new[] { block });

            // Write something to force it to grab a block
            this.stream.Write(new byte[10], 0, 10);
            this.stream.Dispose();

            Assert.That(this.pool.GetBlock(), Is.SameAs(block));
        }

        [Test]
        public void FlushShouldNotThrowAnException()
        {
            Assert.That(() => this.stream.Flush(), Throws.Nothing);
        }

        [Test]
        public void FlushShouldThrowIfDisposed()
        {
            this.stream.Dispose();

            Assert.That(() => this.stream.Flush(), Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void ReadShouldReadAllTheBytes()
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

            Assert.That(read, Is.EqualTo(data.Length));
            Assert.That(buffer[0], Is.EqualTo(0));
            Assert.That(buffer[1], Is.EqualTo(1));
            Assert.That(buffer[buffer.Length - 2], Is.EqualTo(1));
            Assert.That(buffer[buffer.Length - 1], Is.EqualTo(0));
        }

        [Test]
        public void ReadShouldThrowIfDisposed()
        {
            this.stream.Dispose();

            byte[] buffer = new byte[1];
            Assert.That(() => this.stream.Read(buffer, 0, 1), Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void SeekShouldSetThePositionFromTheStart()
        {
            this.stream.Position = 1;

            this.stream.Seek(2, SeekOrigin.Begin);

            Assert.That(this.stream.Position, Is.EqualTo(2L));
        }

        [Test]
        public void SeekShouldSetThePositionFromTheCurrentPosition()
        {
            this.stream.Position = 1;

            this.stream.Seek(2, SeekOrigin.Current);

            Assert.That(this.stream.Position, Is.EqualTo(3L));
        }

        [Test]
        public void SeekShouldSetThePositionFromTheEnd()
        {
            this.stream.SetLength(1);

            this.stream.Seek(2, SeekOrigin.End);

            Assert.That(this.stream.Position, Is.EqualTo(3L));
        }

        [Test]
        public void SeekShouldThrowIfDisposed()
        {
            this.stream.Dispose();

            Assert.That(() => this.stream.Seek(0, SeekOrigin.Begin), Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void SetLengthShouldUpdateTheLength()
        {
            this.stream.SetLength(3);

            Assert.That(stream.Length, Is.EqualTo(3L));
        }

        [Test]
        public void SetLengthShouldEnsurePositionIsSmaller()
        {
            this.stream.Position = 12;

            this.stream.SetLength(5);

            Assert.That(this.stream.Position, Is.EqualTo(5L));
        }

        [Test]
        public void SetLengthShouldThrowIfDisposed()
        {
            this.stream.Dispose();

            Assert.That(() => this.stream.SetLength(0), Throws.InstanceOf<ObjectDisposedException>());
        }

        [Test]
        public void WriteShouldThrowIfDisposed()
        {
            this.stream.Dispose();

            byte[] buffer = new byte[1];
            Assert.That(() => this.stream.Write(buffer, 0, 1), Throws.InstanceOf<ObjectDisposedException>());
        }
    }
}
