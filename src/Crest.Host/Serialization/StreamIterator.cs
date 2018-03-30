// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.Text;
    using static System.Diagnostics.Debug;

    /// <summary>
    /// Allows the iteration over the characters in a stream.
    /// </summary>
    /// <remarks>
    /// This class makes use of shared buffers so it is cheaper to allocate
    /// than a <see cref="StreamReader"/>.
    /// </remarks>
    internal sealed class StreamIterator : ICharIterator, IDisposable
    {
        // We use this value for the buffer lengths as when we rent them they
        // may return a bigger buffer, however, since we're copying from one to
        // another, it's easier if we assume they're the same size
        private const int BufferSize = 4096;
        private readonly Decoder decoder;
        private readonly Stream stream;
        private byte[] byteBuffer;
        private char[] charBuffer;
        private int length;
        private int offset;
        private int previousCharsRead;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamIterator"/> class.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        public StreamIterator(Stream stream)
        {
            this.byteBuffer = BytePool.Rent(BufferSize);
            this.charBuffer = CharPool.Rent(BufferSize);
            this.stream = stream;

            // Do a little read to detect the encoding
            int read = this.stream.Read(this.byteBuffer, 0, BufferSize);
            if (read == 0)
            {
                this.length = -1;
            }
            else
            {
                this.decoder = DetectEncoding(this.byteBuffer, out int skip).GetDecoder();
                this.length = this.decoder.GetChars(this.byteBuffer, skip, read - skip, this.charBuffer, 0);
            }

            // We've already read a little, so let MoveNext increase the offset to get to the start
            this.offset = -1;
        }

        /// <inheritdoc />
        public char Current { get; private set; }

        /// <inheritdoc />
        public int Position => this.previousCharsRead + this.offset + 1;

        /// <summary>
        /// Gets or sets the resource pool for obtaining byte arrays.
        /// </summary>
        /// <remarks>
        /// This is exposed for unit testing and defaults to
        /// <see cref="ArrayPool{T}.Shared"/>.
        /// </remarks>
        internal static ArrayPool<byte> BytePool { get; set; } = ArrayPool<byte>.Shared;

        /// <summary>
        /// Gets or sets the resource pool for obtaining character arrays.
        /// </summary>
        /// <remarks>
        /// This is exposed for unit testing and defaults to
        /// <see cref="ArrayPool{T}.Shared"/>.
        /// </remarks>
        internal static ArrayPool<char> CharPool { get; set; } = ArrayPool<char>.Shared;

        private bool IsEndOfStream
        {
            get => this.length == -1;
            set => this.length = -1;
        }

        /// <summary>
        /// Releases the resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            if (this.byteBuffer != null)
            {
                BytePool.Return(this.byteBuffer);
                this.byteBuffer = null;

                CharPool.Return(this.charBuffer);
                this.charBuffer = null;
            }

            this.stream.Dispose();
        }

        /// <summary>
        /// Reads a specified amount from the stream without advancing the
        /// current position.
        /// </summary>
        /// <param name="maximumBytes">
        /// The maximum amount to read from the stream.
        /// </param>
        /// <returns>A span containing the read data.</returns>
        public ReadOnlySpan<char> GetSpan(int maximumBytes)
        {
            Assert(maximumBytes < BufferSize, "maximumBytes must be less than BufferSize");
            if (this.IsEndOfStream)
            {
                return ReadOnlySpan<char>.Empty;
            }

            int count = this.length - this.offset;
            if (count < maximumBytes)
            {
                this.CompactCharBuffer();
                this.FillBuffer(this.length);
                maximumBytes = Math.Min(maximumBytes, this.length - this.offset);
            }

            return new ReadOnlySpan<char>(this.charBuffer, this.offset, maximumBytes);
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            this.offset++;
            if (this.offset >= this.length)
            {
                if (this.FillBuffer(0))
                {
                    this.offset = 0;
                }
                else
                {
                    this.IsEndOfStream = true;
                    this.Current = default;
                    return false;
                }
            }

            this.Current = this.charBuffer[this.offset];
            return true;
        }

        /// <summary>
        /// Advances the stream by the specified amount.
        /// </summary>
        /// <param name="length">The number of bytes to advance by.</param>
        public void Skip(int length)
        {
            this.offset += length;
            this.Current = this.charBuffer[this.offset];
        }

        private static Encoding DetectEncoding(byte[] bytes, out int skip)
        {
            if ((bytes[0] == 0xFE) && (bytes[1] == 0xFF))
            {
                // UTF-16 Big endian
                skip = 2;
                return Encoding.BigEndianUnicode;
            }

            if ((bytes[0] == 0xFF) && (bytes[1] == 0xFE))
            {
                if ((bytes[2] == 0) && (bytes[3] == 0))
                {
                    // UTF-32 Little endian
                    skip = 4;
                    return Encoding.UTF32;
                }
                else
                {
                    // UTF-16 Little endian
                    skip = 2;
                    return Encoding.Unicode;
                }
            }

            if ((bytes[0] == 0) && (bytes[1] == 0) && (bytes[2] == 0xFE) && (bytes[3] == 0xFF))
            {
                // UTF-32 Big endian
                skip = 4;
                return new UTF32Encoding(bigEndian: true, byteOrderMark: false);
            }

            if ((bytes[0] == 0xEF) && (bytes[1] == 0xBB) && (bytes[2] == 0xBF))
            {
                skip = 3;
            }
            else
            {
                skip = 0;
            }

            return Encoding.UTF8;
        }

        private void CompactCharBuffer()
        {
            this.length -= this.offset;
            Buffer.BlockCopy(
                this.charBuffer,
                this.offset * sizeof(char),
                this.charBuffer,
                0,
                this.length * sizeof(char));

            this.offset = 0;
        }

        private bool FillBuffer(int start)
        {
            if (this.IsEndOfStream)
            {
                return false;
            }

            this.previousCharsRead += this.length;

            // We can't call decoder with more bytes than available buffer
            // space for the characters or it will throw. Be conservative and
            // assume each byte represents one character
            int available = BufferSize - start;
            int read = this.stream.Read(this.byteBuffer, 0, available);
            if (read == 0)
            {
                return false;
            }
            else
            {
                this.length = start + this.decoder.GetChars(this.byteBuffer, 0, read, this.charBuffer, start);
                return true;
            }
        }
    }
}
