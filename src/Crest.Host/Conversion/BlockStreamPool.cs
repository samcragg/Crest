﻿// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using static System.Diagnostics.Debug;

    /// <summary>
    /// Manages the internal buffers for the <see cref="BlockStream"/> classes.
    /// </summary>
    internal sealed class BlockStreamPool
    {
        /// <summary>
        /// The size, in bytes, of the blocks.
        /// </summary>
        internal const int DefaultBlockSize = 64 * 1024;

        /// <summary>
        /// The maximum number of bytes that will be retained by a pool.
        /// </summary>
        internal const int MaximumPoolSize = DefaultBlockSize * 1024;

        private int availableBytes;
        private ImmutableStack<byte[]> pool = ImmutableStack<byte[]>.Empty;

        /// <summary>
        /// Gets a new stream that uses the blocks from this instance.
        /// </summary>
        /// <returns>A new stream.</returns>
        public Stream GetStream()
        {
            return new BlockStream(this);
        }

        /// <summary>
        /// Gets a byte buffer.
        /// </summary>
        /// <returns>A byte array.</returns>
        internal byte[] GetBlock()
        {
            byte[] block;
            if (ImmutableInterlocked.TryPop(ref this.pool, out block))
            {
                Interlocked.Add(ref this.availableBytes, -DefaultBlockSize);
            }
            else
            {
                // We don't have any just yet but we'll try to put this back
                // into the pool when it gets released.
                block = new byte[DefaultBlockSize];
            }

            return block;
        }

        /// <summary>
        /// Returns the blocks returned by <see cref="GetBlock"/> to the pool.
        /// </summary>
        /// <param name="blocks">The byte arrays returned from this instance.</param>
        internal void ReturnBlocks(IReadOnlyCollection<byte[]> blocks)
        {
            CheckReturnedBlocks(blocks);

            foreach (byte[] block in blocks)
            {
                if (Volatile.Read(ref this.availableBytes) >= MaximumPoolSize)
                {
                    break;
                }

                Interlocked.Add(ref this.availableBytes, DefaultBlockSize);
                ImmutableInterlocked.Push(ref this.pool, block);
            }
        }

        [Conditional("DEBUG")]
        private static void CheckReturnedBlocks(IReadOnlyCollection<byte[]> blocks)
        {
            foreach (byte[] block in blocks)
            {
                Assert(block.Length == DefaultBlockSize, "Collection contains buffers that were not created by this instance.");
            }
        }
    }
}
