namespace Host.UnitTests.IO
{
    using System.Collections.Generic;
    using System.IO;
    using Crest.Host.IO;
    using FluentAssertions;
    using Xunit;

    public class BlockStreamPoolTests
    {
        private readonly BlockStreamPool pool = new BlockStreamPool();

        public sealed class GetBlock : BlockStreamPoolTests
        {
            [Fact]
            public void ShouldReturnAByteArray()
            {
                byte[] block = this.pool.GetBlock();

                block.Length.Should().Be(BlockStreamPool.DefaultBlockSize);
            }

            [Fact]
            public void ShouldReturnAByteArrayFromThePoolIfAvailable()
            {
                byte[] first = this.pool.GetBlock();
                this.pool.ReturnBlocks(new[] { first });

                byte[] second = this.pool.GetBlock();

                second.Should().BeSameAs(first);
            }
        }

        public sealed class GetStream : BlockStreamPoolTests
        {
            [Fact]
            public void ShouldReturnANewObject()
            {
                Stream stream1 = this.pool.GetStream();
                Stream stream2 = this.pool.GetStream();

                stream1.Should().NotBeNull();
                stream2.Should().NotBeNull();
                stream1.Should().NotBeSameAs(stream2);
            }
        }

        public sealed class ReturnBlocks : BlockStreamPoolTests
        {
            [Fact]
            public void ShouldNotExceedMaximumPoolSize()
            {
                var blocks = new List<byte[]>();
                int bytes = 0;
                while (bytes < BlockStreamPool.MaximumPoolSize)
                {
                    blocks.Add(new byte[BlockStreamPool.DefaultBlockSize]);
                    bytes += BlockStreamPool.DefaultBlockSize;
                }

                var tooBig = new List<byte[]>
                {
                    new byte[BlockStreamPool.DefaultBlockSize]
                };

                // Fill the pool up with known blocks...
                this.pool.ReturnBlocks(blocks);
                this.pool.ReturnBlocks(tooBig);

                // Verify we get them all back
                for (int i = 0; i < blocks.Count; i++)
                {
                    byte[] block = this.pool.GetBlock();
                    blocks.Should().Contain(block);
                }

                // Check that once the pool is exhausted it is allocating new ones
                // and not using the one that didn't fit
                byte[] nonPoolBlock = this.pool.GetBlock();
                blocks.Should().NotContain(nonPoolBlock);
                tooBig.Should().NotContain(nonPoolBlock);
            }
        }
    }
}
