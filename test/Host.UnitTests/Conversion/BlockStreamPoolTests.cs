namespace Host.UnitTests.Conversion
{
    using System.Collections.Generic;
    using System.IO;
    using Crest.Host.Conversion;
    using NUnit.Framework;

    [TestFixture]
    public sealed class BlockStreamPoolTests
    {
        private BlockStreamPool pool;

        [SetUp]
        public void SetUp()
        {
            this.pool = new BlockStreamPool();
        }

        [Test]
        public void GetStreamShouldReturnANewObject()
        {
            Stream stream1 = this.pool.GetStream();
            Stream stream2 = this.pool.GetStream();

            Assert.That(stream1, Is.Not.Null);
            Assert.That(stream2, Is.Not.Null);
            Assert.That(stream1, Is.Not.SameAs(stream2));
        }

        [Test]
        public void GetBlockShouldReturnAByteArray()
        {
            byte[] block = this.pool.GetBlock();

            Assert.That(block.Length, Is.EqualTo(BlockStreamPool.DefaultBlockSize));
        }

        [Test]
        public void GetBlockShouldReturnAByteArrayFromThePoolIfAvailable()
        {
            byte[] first = this.pool.GetBlock();
            this.pool.ReturnBlocks(new[] { first });

            byte[] second = this.pool.GetBlock();

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void ReturnBlocksShouldNotExceedMaximumPoolSize()
        {
            var blocks = new List<byte[]>();
            int bytes = 0;
            while (bytes < BlockStreamPool.MaximumPoolSize)
            {
                blocks.Add(new byte[BlockStreamPool.DefaultBlockSize]);
                bytes += BlockStreamPool.DefaultBlockSize;
            }

            var tooBig = new List<byte[]>();
            tooBig.Add(new byte[BlockStreamPool.DefaultBlockSize]);

            // Fill the pool up with known blocks...
            this.pool.ReturnBlocks(blocks);
            this.pool.ReturnBlocks(tooBig);

            // Verify we get them all back
            for (int i = 0; i < blocks.Count; i++)
            {
                byte[] block = this.pool.GetBlock();
                Assert.That(blocks.Contains(block), Is.True);
            }

            // Check that once the pool is exhausted it is allocating new ones
            // and not using the one that didn't fit
            byte[] nonPoolBlock = this.pool.GetBlock();
            Assert.That(blocks.Contains(nonPoolBlock), Is.False);
            Assert.That(tooBig.Contains(nonPoolBlock), Is.False);
        }
    }
}
