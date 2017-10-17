// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;

    /// <summary>
    /// Writes values to the underlying stream.
    /// </summary>
    [CLSCompliant(false)]
    public interface IStreamWriter
    {
        /// <summary>
        /// Clears all buffers for the current writer and causes any buffered
        /// data to be written to the underlying stream.
        /// </summary>
        void Flush();

        /// <summary>
        /// Writes a boolean to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteBoolean(bool value);

        /// <summary>
        /// Writes an 8-bit unsigned integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteByte(byte value);

        /// <summary>
        /// Writes a Unicode character to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteChar(char value);

        /// <summary>
        /// Writes a <see cref="DateTime"/> to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteDateTime(DateTime value);

        /// <summary>
        /// Writes a <see cref="decimal"/> to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteDecimal(decimal value);

        /// <summary>
        /// Writes a double-precision floating-point number to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteDouble(double value);

        /// <summary>
        /// Writes a GUID to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteGuid(Guid value);

        /// <summary>
        /// Writes a 16-bit signed integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteInt16(short value);

        /// <summary>
        /// Writes a 32-bit signed integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteInt32(int value);

        /// <summary>
        /// Writes a 64-bit signed integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteInt64(long value);

        /// <summary>
        /// Writes a token to the stream indicating there is no value.
        /// </summary>
        void WriteNull();

        /// <summary>
        /// Writes an object to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteObject(object value);

        /// <summary>
        /// Writes an 8-bit signed integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteSByte(sbyte value);

        /// <summary>
        /// Writes a single-precision floating-point number to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteSingle(float value);

        /// <summary>
        /// Writes a string of characters to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteString(string value);

        /// <summary>
        /// Writes a <see cref="TimeSpan"/> to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteTimeSpan(TimeSpan value);

        /// <summary>
        /// Writes a 16-bit unsigned integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteUInt16(ushort value);

        /// <summary>
        /// Writes a 32-bit unsigned integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteUInt32(uint value);

        /// <summary>
        /// Writes a 64-bit unsigned integer to the stream.
        /// </summary>
        /// <param name="value">The value to write to the stream.</param>
        void WriteUInt64(ulong value);
    }
}
