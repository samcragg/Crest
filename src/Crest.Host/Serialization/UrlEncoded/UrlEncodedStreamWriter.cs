// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.UrlEncoded
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using Crest.Host.Conversion;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Used to output primitive values that are URL encoded.
    /// </summary>
    internal sealed class UrlEncodedStreamWriter : ValueWriter
    {
        private const int BufferLength = 1024;
        private static readonly byte[] FalseValue = { (byte)'f', (byte)'a', (byte)'l', (byte)'s', (byte)'e' };
        private static readonly byte[] NullValue = { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };
        private static readonly byte[] TrueValue = { (byte)'t', (byte)'r', (byte)'u', (byte)'e' };
        private static readonly Encoder Utf8Encoder = Encoding.UTF8.GetEncoder();

        private readonly byte[] buffer = new byte[BufferLength];
        private readonly List<byte[]> keyParts = new List<byte[]>(); // We need to iterate from first-to-last, so can't use Stack :(
        private readonly Stream stream;
        private bool hasKeyWritten;
        private int keyLength;
        private int offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlEncodedStreamWriter"/> class.
        /// </summary>
        /// <param name="stream">The stream to output the values to.</param>
        public UrlEncodedStreamWriter(Stream stream)
        {
            this.stream = stream;
        }

        /// <inheritdoc />
        public override void Flush()
        {
            this.stream.Write(this.buffer, 0, this.offset);
            this.offset = 0;
        }

        /// <inheritdoc />
        public override void WriteBoolean(bool value)
        {
            this.WriteCurrentProperty();
            this.EnsureBufferHasSpace(5); // The longest this method will write is "false"

            if (value)
            {
                Buffer.BlockCopy(TrueValue, 0, this.buffer, this.offset, 4);
                this.offset += 4;
            }
            else
            {
                Buffer.BlockCopy(FalseValue, 0, this.buffer, this.offset, 5);
                this.offset += 5;
            }
        }

        /// <inheritdoc />
        public override void WriteChar(char value)
        {
            this.WriteCurrentProperty();
            this.EnsureBufferHasSpace(UrlStringEncoding.MaxBytesPerCharacter);
            this.offset += UrlStringEncoding.AppendChar(
                UrlStringEncoding.SpaceOption.Plus,
                value,
                this.buffer.AsSpan(this.offset));
        }

        /// <inheritdoc />
        public override void WriteNull()
        {
            this.WriteCurrentProperty();
            this.EnsureBufferHasSpace(4);

            Buffer.BlockCopy(NullValue, 0, this.buffer, this.offset, 4);
            this.offset += 4;
        }

        /// <inheritdoc />
        public override void WriteString(string value)
        {
            this.WriteCurrentProperty();
            Span<byte> span = this.buffer.AsSpan();
            for (int i = 0; i < value.Length; i++)
            {
                this.EnsureBufferHasSpace(UrlStringEncoding.MaxBytesPerCharacter);
                this.offset += UrlStringEncoding.AppendChar(
                    UrlStringEncoding.SpaceOption.Plus,
                    value,
                    ref i,
                    span.Slice(this.offset));
            }
        }

        /// <inheritdoc />
        public override void WriteUri(Uri value)
        {
            if (value.IsAbsoluteUri)
            {
                // AbsoluteUri escapes it for us, so just write the raw string
                this.WriteCurrentProperty();
                this.WriteRawString(value.AbsoluteUri);
            }
            else
            {
                // We need to escape this
                this.WriteString(value.OriginalString);
            }
        }

        /// <summary>
        /// Removes the most recently added key part.
        /// </summary>
        internal void PopKeyPart()
        {
            int end = this.keyParts.Count - 1;
            this.keyLength -= this.keyParts[end].Length + 1;
            this.keyParts.RemoveAt(end);
        }

        /// <summary>
        /// Adds the specified name to the end of the current key.
        /// </summary>
        /// <param name="name">The encoded bytes of the key.</param>
        internal void PushKeyPart(byte[] name)
        {
            this.keyParts.Add(name);
            this.keyLength += name.Length + 1; // +1 for . or =
        }

        /// <summary>
        /// Adds the specified array index to the end of the current key.
        /// </summary>
        /// <param name="arrayIndex">The index of the current array item.</param>
        internal void PushKeyPart(int arrayIndex)
        {
            int digits = IntegerConverter.CountDigits((uint)arrayIndex);
            byte[] text = new byte[digits];
            IntegerConverter.WriteUInt64(new Span<byte>(text), (uint)arrayIndex);

            this.keyParts.Add(text);
            this.keyLength += digits + 1;
        }

        /// <inheritdoc />
        protected override void CommitBuffer(int bytes)
        {
            this.offset += bytes;
        }

        /// <inheritdoc />
        protected override Span<byte> RentBuffer(int maximumSize)
        {
            this.WriteCurrentProperty();
            this.EnsureBufferHasSpace(maximumSize);
            return new Span<byte>(this.buffer, this.offset, BufferLength - this.offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CopyBytes(byte[] source, byte[] destination, int destinationOffset)
        {
            Buffer.BlockCopy(source, 0, destination, destinationOffset, source.Length);
            return destinationOffset + source.Length;
        }

        private static unsafe void WriteStringToBuffer(char* charPtr, int length, byte[] byteBuffer, int bufferOffset)
        {
            fixed (byte* bufferPtr = byteBuffer)
            {
                Utf8Encoder.GetBytes(
                    charPtr,
                    length,
                    bufferPtr + bufferOffset,
                    byteBuffer.Length - bufferOffset,
                    flush: true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureBufferHasSpace(int amount)
        {
            if ((this.offset + amount) > BufferLength)
            {
                this.Flush();
            }
        }

        private void WriteCurrentProperty()
        {
            if (this.keyParts.Count == 0)
            {
                return;
            }

            // +1 in case we need the & first
            this.EnsureBufferHasSpace(this.keyLength + 1);
            if (this.hasKeyWritten)
            {
                this.buffer[this.offset++] = (byte)'&';
            }

            this.hasKeyWritten = true;

            this.offset = CopyBytes(this.keyParts[0], this.buffer, this.offset);
            for (int i = 1; i < this.keyParts.Count; i++)
            {
                this.buffer[this.offset] = (byte)'.';
                this.offset = CopyBytes(this.keyParts[i], this.buffer, this.offset + 1);
            }

            this.buffer[this.offset++] = (byte)'=';
        }

        private unsafe void WriteRawString(string value)
        {
            fixed (char* charPtr = value)
            {
                int encodedByteLength = Utf8Encoder.GetByteCount(charPtr, value.Length, false);
                if (encodedByteLength < BufferLength)
                {
                    this.EnsureBufferHasSpace(encodedByteLength);
                    WriteStringToBuffer(charPtr, value.Length, this.buffer, this.offset);
                    this.offset += encodedByteLength;
                }
                else
                {
                    this.Flush();
                    byte[] byteBuffer = new byte[encodedByteLength];
                    WriteStringToBuffer(charPtr, value.Length, byteBuffer, 0);
                    this.stream.Write(byteBuffer, 0, byteBuffer.Length);
                }
            }
        }
    }
}
