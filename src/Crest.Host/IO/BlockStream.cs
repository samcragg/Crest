// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Gets a stream that stores its contents in memory in a series of blocks.
    /// </summary>
    internal sealed class BlockStream : Stream
    {
        private readonly List<byte[]> blocks = new List<byte[]>(capacity: 1);
        private readonly BlockStreamPool pool;
        private bool isDisposed;
        private int length;
        private int position;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockStream"/> class.
        /// </summary>
        /// <param name="pool">The instance used to get the blocks from.</param>
        internal BlockStream(BlockStreamPool pool)
        {
            this.pool = pool;
        }

        /// <inheritdoc />
        public override bool CanRead
        {
            get
            {
                this.ThrowIfDisposed();
                return true;
            }
        }

        /// <inheritdoc />
        public override bool CanSeek
        {
            get
            {
                this.ThrowIfDisposed();
                return true;
            }
        }

        /// <inheritdoc />
        public override bool CanWrite
        {
            get
            {
                this.ThrowIfDisposed();
                return true;
            }
        }

        /// <inheritdoc />
        public override long Length
        {
            get
            {
                this.ThrowIfDisposed();
                return this.length;
            }
        }

        /// <inheritdoc />
        public override long Position
        {
            get => this.position;
            set => this.position = (int)value;
        }

        /// <inheritdoc />
        public override void Flush()
        {
            this.ThrowIfDisposed();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            this.ThrowIfDisposed();

            if (this.blocks.Count == 0)
            {
                return 0;
            }

            int read = 0;
            int blockIndex = DivRem(this.position, BlockStreamPool.DefaultBlockSize, out int blockOffset);
            int remaining = Math.Min(count, this.length - this.position);
            while (remaining > 0)
            {
                byte[] block = this.blocks[blockIndex];
                int available = BlockStreamPool.DefaultBlockSize - blockOffset;

                int amount = Math.Min(available, remaining);
                Buffer.BlockCopy(block, blockOffset, buffer, offset, amount);

                blockIndex++;
                blockOffset = 0;
                offset += amount;
                remaining -= amount;
                read += amount;
            }

            this.position += read;
            return read;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            this.ThrowIfDisposed();

            switch (origin)
            {
                case SeekOrigin.Begin:
                    this.position = (int)offset;
                    break;

                case SeekOrigin.Current:
                    this.position += (int)offset;
                    break;

                case SeekOrigin.End:
                    this.position = this.length + (int)offset;
                    break;
            }

            return this.Position;
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            this.ThrowIfDisposed();
            this.length = (int)value;
            this.EnsureCapacity(this.length);

            if (this.Position > value)
            {
                this.Position = value;
            }
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.ThrowIfDisposed();
            this.EnsureCapacity((int)(this.Position + count));

            int blockIndex = DivRem(this.position, BlockStreamPool.DefaultBlockSize, out int blockOffset);
            int remaining = count;
            while (remaining > 0)
            {
                byte[] block = this.blocks[blockIndex];
                int available = BlockStreamPool.DefaultBlockSize - blockOffset;

                int amount = Math.Min(available, remaining);
                Buffer.BlockCopy(buffer, offset, block, blockOffset, amount);

                blockIndex++;
                blockOffset = 0;
                remaining -= amount;
            }

            this.position += count;
            this.length = Math.Max(this.position, this.length);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.isDisposed = true;
            this.pool.ReturnBlocks(this.blocks);
            this.blocks.Clear();
        }

        // System.Math.DivRem is missing from .NET Standard 1.6 :(
        private static int DivRem(int a, int b, out int result)
        {
            result = a % b;
            return a / b;
        }

        private void EnsureCapacity(int value)
        {
            while ((this.blocks.Count * BlockStreamPool.DefaultBlockSize) < value)
            {
                this.blocks.Add(this.pool.GetBlock());
            }
        }

        private void ThrowIfDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(nameof(BlockStream));
            }
        }
    }
}
