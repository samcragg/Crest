// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using Crest.Host.Conversion;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Used to output JSON primitive values.
    /// </summary>
    internal sealed class JsonStreamWriter : ValueWriter
    {
        private const int BufferLength = 1024;
        private static readonly byte[] FalseValue = { (byte)'f', (byte)'a', (byte)'l', (byte)'s', (byte)'e' };
        private static readonly byte[] NullValue = { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };
        private static readonly byte[] TrueValue = { (byte)'t', (byte)'r', (byte)'u', (byte)'e' };

        private readonly byte[] buffer = new byte[BufferLength];
        private readonly Stream stream;
        private int offset;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStreamWriter"/> class.
        /// </summary>
        /// <param name="stream">The stream to output the values to.</param>
        public JsonStreamWriter(Stream stream)
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
            this.EnsureBufferHasSpace(JsonStringEncoding.MaxBytesPerCharacter + 2); // +2 for the surrounding quotes

            this.buffer[this.offset++] = (byte)'"';
            JsonStringEncoding.AppendChar(value, this.buffer, ref this.offset);
            this.buffer[this.offset++] = (byte)'"';
        }

        /// <inheritdoc />
        public override void WriteDecimal(decimal value)
        {
            this.WriteRaw(value.ToString("G", NumberFormatInfo.InvariantInfo));
        }

        /// <inheritdoc />
        public override void WriteDouble(double value)
        {
            if (double.IsInfinity(value) || double.IsNaN(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "JSON output cannot contain infinite/NaN values");
            }

            this.WriteRaw(value.ToString("G", NumberFormatInfo.InvariantInfo));
        }

        /// <inheritdoc />
        public override void WriteInt64(long value)
        {
            this.EnsureBufferHasSpace(IntegerConverter.MaximumTextLength);

            this.offset += IntegerConverter.WriteInt64(this.buffer, this.offset, value);
        }

        /// <inheritdoc />
        public override void WriteNull()
        {
            this.EnsureBufferHasSpace(4);
            Buffer.BlockCopy(NullValue, 0, this.buffer, this.offset, 4);
            this.offset += 4;
        }

        /// <inheritdoc />
        public override void WriteSingle(float value)
        {
            if (float.IsInfinity(value) || float.IsNaN(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "JSON output cannot contain infinite/NaN values");
            }

            this.WriteRaw(value.ToString("G", NumberFormatInfo.InvariantInfo));
        }

        /// <inheritdoc />
        public override void WriteString(string value)
        {
            this.AppendByte((byte)'"');
            for (int i = 0; i < value.Length; i++)
            {
                this.EnsureBufferHasSpace(JsonStringEncoding.MaxBytesPerCharacter);
                JsonStringEncoding.AppendChar(value, ref i, this.buffer, ref this.offset);
            }

            this.AppendByte((byte)'"');
        }

        /// <inheritdoc />
        public override void WriteUInt64(ulong value)
        {
            this.EnsureBufferHasSpace(IntegerConverter.MaximumTextLength);

            this.offset += IntegerConverter.WriteUInt64(this.buffer, this.offset, value);
        }

        /// <summary>
        /// Writes a raw byte to the end of the stream.
        /// </summary>
        /// <param name="value">The value to write.</param>
        internal void AppendByte(byte value)
        {
            this.EnsureBufferHasSpace(1);
            this.buffer[this.offset++] = value;
        }

        /// <summary>
        /// Writes a series of raw bytes to the end of the stream.
        /// </summary>
        /// <param name="bytes">The values to write.</param>
        internal void AppendBytes(byte[] bytes)
        {
            this.EnsureBufferHasSpace(bytes.Length);
            Buffer.BlockCopy(bytes, 0, this.buffer, this.offset, bytes.Length);
            this.offset += bytes.Length;
        }

        /// <inheritdoc />
        protected override void CommitBuffer(int bytes)
        {
            this.offset += bytes;
            this.buffer[this.offset++] = (byte)'"';
        }

        /// <inheritdoc />
        protected override ArraySegment<byte> RentBuffer(int maximumSize)
        {
            this.EnsureBufferHasSpace(DateTimeConverter.MaximumTextLength + 2); // +2 for the surrounding quotes
            this.buffer[this.offset++] = (byte)'"';
            return new ArraySegment<byte>(this.buffer, this.offset, BufferLength - this.offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureBufferHasSpace(int amount)
        {
            if ((this.offset + amount) > BufferLength)
            {
                this.Flush();
            }
        }

        private void WriteRaw(string value)
        {
            this.EnsureBufferHasSpace(value.Length);

            int index = this.offset;
            for (int i = 0; i < value.Length; i++)
            {
                this.buffer[index] = (byte)value[i];
                index++;
            }

            this.offset += value.Length;
        }
    }
}
