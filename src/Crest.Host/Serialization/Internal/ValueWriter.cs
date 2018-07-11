// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Internal
{
    using System;
    using System.Globalization;
    using Crest.Host.Conversion;
    using SCM = System.ComponentModel;

    /// <summary>
    /// Writes values to the underlying stream.
    /// </summary>
    public abstract class ValueWriter
    {
        /// <summary>
        /// Clears all buffers for the current writer and causes any buffered
        /// data to be written to the underlying stream.
        /// </summary>
        public abstract void Flush();

        /// <summary>
        /// Writes a boolean to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        public abstract void WriteBoolean(bool value);

        /// <summary>
        /// Writes an 8-bit unsigned integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        public virtual void WriteByte(byte value)
        {
            this.WriteUInt64(value);
        }

        /// <summary>
        /// Writes a Unicode character to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        public abstract void WriteChar(char value);

        /// <summary>
        /// Writes a <see cref="DateTime"/> to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        public virtual void WriteDateTime(DateTime value)
        {
            ArraySegment<byte> buffer = this.RentBuffer(DateTimeConverter.MaximumTextLength);
            int length = DateTimeConverter.WriteDateTime(buffer.Array, buffer.Offset, value);
            this.CommitBuffer(length);
        }

        /// <summary>
        /// Writes a <see cref="decimal"/> to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        public virtual void WriteDecimal(decimal value)
        {
            this.AppendAscii(value.ToString("G", NumberFormatInfo.InvariantInfo));
        }

        /// <summary>
        /// Writes a double-precision floating-point number to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        public virtual void WriteDouble(double value)
        {
            this.AppendAscii(value.ToString("G", NumberFormatInfo.InvariantInfo));
        }

        /// <summary>
        /// Writes a GUID to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        public virtual void WriteGuid(Guid value)
        {
            ArraySegment<byte> buffer = this.RentBuffer(GuidConverter.MaximumTextLength);
            int length = GuidConverter.WriteGuid(buffer.Array, buffer.Offset, value);
            this.CommitBuffer(length);
        }

        /// <summary>
        /// Writes a 16-bit signed integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        public void WriteInt16(short value)
        {
            this.WriteInt64(value);
        }

        /// <summary>
        /// Writes a 32-bit signed integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        public virtual void WriteInt32(int value)
        {
            this.WriteInt64(value);
        }

        /// <summary>
        /// Writes a 64-bit signed integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        public virtual void WriteInt64(long value)
        {
            ArraySegment<byte> buffer = this.RentBuffer(IntegerConverter.MaximumTextLength);
            int length = IntegerConverter.WriteInt64(buffer.Array, buffer.Offset, value);
            this.CommitBuffer(length);
        }

        /// <summary>
        /// Writes a token to the stream indicating there is no value.
        /// </summary>
        public abstract void WriteNull();

        /// <summary>
        /// Writes an object to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        public virtual void WriteObject(object value)
        {
            SCM.TypeConverter converter = SCM.TypeDescriptor.GetConverter(value);
            string converted = converter.ConvertToInvariantString(value);
            this.WriteString(converted);
        }

        /// <summary>
        /// Writes an 8-bit signed integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        [CLSCompliant(false)]
        public virtual void WriteSByte(sbyte value)
        {
            this.WriteInt64(value);
        }

        /// <summary>
        /// Writes a single-precision floating-point number to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        public virtual void WriteSingle(float value)
        {
            this.AppendAscii(value.ToString("G", NumberFormatInfo.InvariantInfo));
        }

        /// <summary>
        /// Writes a string of characters to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        public abstract void WriteString(string value);

        /// <summary>
        /// Writes a <see cref="TimeSpan"/> to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        public virtual void WriteTimeSpan(TimeSpan value)
        {
            ArraySegment<byte> buffer = this.RentBuffer(TimeSpanConverter.MaximumTextLength);
            int length = TimeSpanConverter.WriteTimeSpan(buffer.Array, buffer.Offset, value);
            this.CommitBuffer(length);
        }

        /// <summary>
        /// Writes a 16-bit unsigned integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        [CLSCompliant(false)]
        public virtual void WriteUInt16(ushort value)
        {
            this.WriteUInt64(value);
        }

        /// <summary>
        /// Writes a 32-bit unsigned integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        [CLSCompliant(false)]
        public void WriteUInt32(uint value)
        {
            this.WriteUInt64(value);
        }

        /// <summary>
        /// Writes a 64-bit unsigned integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        [CLSCompliant(false)]
        public virtual void WriteUInt64(ulong value)
        {
            ArraySegment<byte> buffer = this.RentBuffer(IntegerConverter.MaximumTextLength);
            int length = IntegerConverter.WriteUInt64(buffer.Array, buffer.Offset, value);
            this.CommitBuffer(length);
        }

        /// <summary>
        /// Commits the specified number of bytes written to the buffer
        /// returned by <see cref="RentBuffer(int)"/>.
        /// </summary>
        /// <param name="bytes">The number of bytes used in the buffer.</param>
        protected abstract void CommitBuffer(int bytes);

        /// <summary>
        /// Gets a buffer that can be written to.
        /// </summary>
        /// <param name="maximumSize">
        /// The maximum number of bytes that will be written.
        /// </param>
        /// <returns>The buffer to write to.</returns>
        protected abstract ArraySegment<byte> RentBuffer(int maximumSize);

        private void AppendAscii(string value)
        {
            Span<byte> buffer = this.RentBuffer(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                buffer[i] = (byte)value[i];
            }

            this.CommitBuffer(value.Length);
        }
    }
}
