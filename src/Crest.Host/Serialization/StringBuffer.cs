// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Buffers;

    /// <summary>
    /// Allows the quick creation of strings from a series of characters.
    /// </summary>
    /// <remarks>
    /// Uses buffers obtained from the shared <see cref="ArrayPool{T}"/>.
    /// </remarks>
    internal sealed class StringBuffer : IDisposable
    {
        private const int DefaultBufferSize = 4096;
        private char[] buffer;
        private int offset;
        private StringBuffer previous;
        private int start;
        private int totalLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBuffer"/> class.
        /// </summary>
        public StringBuffer()
        {
            this.buffer = Pool.Rent(DefaultBufferSize);
        }

        private StringBuffer(StringBuffer next)
        {
            this.previous = next.previous;
            this.offset = next.offset;
            this.buffer = next.buffer;
        }

        /// <summary>
        /// Gets the number of characters added to this instance.
        /// </summary>
        public int Length => this.totalLength;

        /// <summary>
        /// Gets or sets the resource pool to obtain the buffers from.
        /// </summary>
        /// <remarks>
        /// This is exposed for testing and default to <see cref="ArrayPool{T}.Shared"/>.
        /// </remarks>
        internal static ArrayPool<char> Pool { get; set; } = ArrayPool<char>.Shared;

        /// <summary>
        /// Appends the specified character to the end of the string.
        /// </summary>
        /// <param name="c">The character to append.</param>
        public void Append(char c)
        {
            if (this.offset == this.buffer.Length)
            {
                this.PrependBlock();
            }

            this.buffer[this.offset++] = c;
            this.totalLength++;
        }

        /// <summary>
        /// Appends the specified characters to the end of the string.
        /// </summary>
        /// <param name="value">The characters to append.</param>
        public void Append(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            int remaining = this.buffer.Length - this.offset;
            int index = 0;
            int count = value.Length;
            while (remaining < count)
            {
                value.CopyTo(index, this.buffer, this.offset, remaining);
                this.offset += remaining;
                index += remaining;
                count -= remaining;

                this.PrependBlock();
                remaining = this.buffer.Length;
            }

            value.CopyTo(index, this.buffer, this.offset, count);
            this.offset += count;
            this.totalLength += value.Length;
        }

        /// <summary>
        /// Clears the contents of this instance.
        /// </summary>
        public void Clear()
        {
            if (this.previous != null)
            {
                this.previous.Dispose();
                this.previous = null;
            }

            this.totalLength = 0;
            this.offset = 0;
            this.start = 0;
        }

        /// <summary>
        /// Creates a span that represents the used part of the buffer.
        /// </summary>
        /// <returns>A span containing the string characters.</returns>
        public ReadOnlySpan<char> CreateSpan()
        {
            if (this.totalLength == 0)
            {
                return ReadOnlySpan<char>.Empty;
            }
            else if (this.previous == null)
            {
                return new ReadOnlySpan<char>(this.buffer, this.start, this.totalLength);
            }
            else
            {
                var span = new Span<char>(new char[this.totalLength]);
                CopyTo(this, span);
                return span;
            }
        }

        /// <summary>
        /// Releases the pool resources obtained by this instance.
        /// </summary>
        public void Dispose()
        {
            if (this.buffer != null)
            {
                Pool.Return(this.buffer);
                this.buffer = null;
                this.previous?.Dispose();
            }
        }

        /// <summary>
        /// Returns a string containing all the characters appended to this instance.
        /// </summary>
        /// <returns>The string represented by this instance.</returns>
        public override string ToString()
        {
            if (this.totalLength == 0)
            {
                return string.Empty;
            }
            else
            {
                unsafe
                {
                    string output = new string(default, this.totalLength);
                    fixed (char* outputPtr = output)
                    {
                        CopyTo(this, new Span<char>(outputPtr, output.Length));
                    }

                    return output;
                }
            }
        }

        /// <summary>
        /// Skips characters at the start and end of the string that match the
        /// the specified predicate.
        /// </summary>
        /// <param name="predicate">
        /// Used to indicate whether the character should be removed.
        /// </param>
        public void TrimEnds(Func<char, bool> predicate)
        {
            TrimStart(this, ref this.totalLength, predicate);
            TrimEnd(this, ref this.totalLength, predicate);
        }

        /// <summary>
        /// Removes the specified number of characters from the end of the buffer.
        /// </summary>
        /// <param name="amount">The number of characters to remove.</param>
        public void Truncate(int amount)
        {
            Truncate(this, ref this.totalLength, amount);
        }

        private static unsafe void CopyTo(StringBuffer tail, in Span<char> destination)
        {
            int destinationOffset = destination.Length;
            int destinationLengthBytes = destination.Length * sizeof(char);
            fixed (char* destPtr = &destination[0])
            {
                StringBuffer sb = tail;
                do
                {
                    int start = sb.start;
                    int length = sb.offset - start;
                    destinationOffset -= length;

                    //// TODO: When performance improves use this:
                    //// var buffer = new Span<char>(sb.buffer, start, length);
                    //// buffer.CopyTo(destination.Slice(destinatioinOffset));

                    fixed (char* bufferPtr = sb.buffer)
                    {
                        Buffer.MemoryCopy(
                            bufferPtr + start,
                            destPtr + destinationOffset,
                            destinationLengthBytes,
                            length * sizeof(char));
                    }

                    sb = sb.previous;
                }
                while (sb != null);
            }
        }

        private static void TrimEnd(StringBuffer buffer, ref int totalLength, Func<char, bool> predicate)
        {
            int count = 0;
            for (int i = buffer.offset - 1; i >= buffer.start; i--)
            {
                if (!predicate(buffer.buffer[i]))
                {
                    break;
                }

                count++;
            }

            buffer.offset -= count;
            totalLength -= count;

            // Did we trim the whole chunk?
            if ((buffer.previous != null) && (buffer.start == buffer.offset))
            {
                TrimEnd(buffer.previous, ref totalLength, predicate);
            }
        }

        private static void TrimStart(StringBuffer buffer, ref int totalLength, Func<char, bool> predicate)
        {
            // Start from the beginning chunk
            StringBuffer previous = buffer.previous;
            if (previous != null)
            {
                TrimStart(previous, ref totalLength, predicate);

                // If it didn't trim the whole chunk then there's no need for
                // us to do anything on this one
                if (previous.start < previous.offset)
                {
                    return;
                }
            }

            int count = 0;
            for (int i = buffer.start; i < buffer.offset; i++)
            {
                if (!predicate(buffer.buffer[i]))
                {
                    break;
                }

                count++;
            }

            buffer.start += count;
            totalLength -= count;
        }

        private static void Truncate(StringBuffer buffer, ref int totalLength, int amount)
        {
            int count = Math.Min(buffer.offset - buffer.start, amount);
            buffer.offset -= count;
            totalLength -= count;
            amount -= count;

            // Did we trim the whole chunk?
            if ((buffer.previous != null) && (amount > 0))
            {
                Truncate(buffer.previous, ref totalLength, amount);
            }
        }

        private void PrependBlock()
        {
            // We're full so we need a new buffer
            this.previous = new StringBuffer(this);
            this.buffer = Pool.Rent(DefaultBufferSize);
            this.offset = 0;
        }
    }
}
