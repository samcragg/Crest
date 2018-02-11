// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.CompilerServices;
    using Crest.Host.Conversion;
    using SCM = System.ComponentModel;

    /// <summary>
    /// Used to output primitive values that are URL encoded.
    /// </summary>
    internal sealed class UrlEncodedStreamWriter : IStreamWriter
    {
        /// <summary>
        /// Represents the maximum number of bytes a single character will be
        /// encoded to.
        /// </summary>
        internal const int MaxBytesPerCharacter = 12; // %AA%BB%CC%DD

        private const int BufferLength = 1024;
        private static readonly byte[] FalseValue = { (byte)'f', (byte)'a', (byte)'l', (byte)'s', (byte)'e' };
        private static readonly byte[] NullValue = { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };
        private static readonly byte[] TrueValue = { (byte)'t', (byte)'r', (byte)'u', (byte)'e' };

        private readonly byte[] buffer = new byte[BufferLength];
        private readonly List<byte[]> keyParts = new List<byte[]>(); // We need to iterate from first-to-last, so can't use Stack :(
        private readonly Stream stream;
        private readonly byte[] utf8Buffer = new byte[4];
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
        public void Flush()
        {
            this.stream.Write(this.buffer, 0, this.offset);
            this.offset = 0;
        }

        /// <inheritdoc />
        public void WriteBoolean(bool value)
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
        public void WriteByte(byte value)
        {
            this.WriteUInt64(value);
        }

        /// <inheritdoc />
        public void WriteChar(char value)
        {
            this.WriteCurrentProperty();
            this.EnsureBufferHasSpace(MaxBytesPerCharacter);

            if (TryAppendChar(this.buffer, this.offset, value))
            {
                this.offset++;
            }
            else
            {
                this.offset += AppendUtf32(this.utf8Buffer, this.buffer, this.offset, value);
            }
        }

        /// <inheritdoc />
        public void WriteDateTime(DateTime value)
        {
            this.WriteCurrentProperty();
            this.EnsureBufferHasSpace(DateTimeConverter.MaximumTextLength);

            this.offset += DateTimeConverter.WriteDateTime(this.buffer, this.offset, value);
        }

        /// <inheritdoc />
        public void WriteDecimal(decimal value)
        {
            this.WriteCurrentProperty();

            string text = value.ToString("G", NumberFormatInfo.InvariantInfo);
            this.AppendAscii(text);
        }

        /// <inheritdoc />
        public void WriteDouble(double value)
        {
            this.WriteCurrentProperty();

            string text = value.ToString("G", NumberFormatInfo.InvariantInfo);
            this.AppendAscii(text);
        }

        /// <inheritdoc />
        public void WriteGuid(Guid value)
        {
            this.WriteCurrentProperty();
            this.EnsureBufferHasSpace(GuidConverter.MaximumTextLength);

            this.offset += GuidConverter.WriteGuid(this.buffer, this.offset, value);
        }

        /// <inheritdoc />
        public void WriteInt16(short value)
        {
            this.WriteInt64(value);
        }

        /// <inheritdoc />
        public void WriteInt32(int value)
        {
            this.WriteInt64(value);
        }

        /// <inheritdoc />
        public void WriteInt64(long value)
        {
            this.WriteCurrentProperty();
            this.EnsureBufferHasSpace(IntegerConverter.MaximumTextLength);

            this.offset += IntegerConverter.WriteInt64(this.buffer, this.offset, value);
        }

        /// <inheritdoc />
        public void WriteNull()
        {
            this.WriteCurrentProperty();
            this.EnsureBufferHasSpace(4);

            Buffer.BlockCopy(NullValue, 0, this.buffer, this.offset, 4);
            this.offset += 4;
        }

        /// <inheritdoc />
        public void WriteObject(object value)
        {
            SCM.TypeConverter converter = SCM.TypeDescriptor.GetConverter(value);
            string converted = converter.ConvertToInvariantString(value);
            this.WriteString(converted);
        }

        /// <inheritdoc />
        public void WriteSByte(sbyte value)
        {
            this.WriteInt64(value);
        }

        /// <inheritdoc />
        public void WriteSingle(float value)
        {
            this.WriteCurrentProperty();

            string text = value.ToString("G", NumberFormatInfo.InvariantInfo);
            this.AppendAscii(text);
        }

        /// <inheritdoc />
        public void WriteString(string value)
        {
            this.WriteCurrentProperty();

            for (int i = 0; i < value.Length; i++)
            {
                this.EnsureBufferHasSpace(MaxBytesPerCharacter);
                this.offset += AppendChar(value, ref i, this.buffer, this.offset);
            }
        }

        /// <inheritdoc />
        public void WriteTimeSpan(TimeSpan value)
        {
            this.WriteCurrentProperty();
            this.EnsureBufferHasSpace(TimeSpanConverter.MaximumTextLength);

            this.offset += TimeSpanConverter.WriteTimeSpan(this.buffer, this.offset, value);
        }

        /// <inheritdoc />
        public void WriteUInt16(ushort value)
        {
            this.WriteUInt64(value);
        }

        /// <inheritdoc />
        public void WriteUInt32(uint value)
        {
            this.WriteUInt64(value);
        }

        /// <inheritdoc />
        public void WriteUInt64(ulong value)
        {
            this.WriteCurrentProperty();
            this.EnsureBufferHasSpace(IntegerConverter.MaximumTextLength);

            this.offset += IntegerConverter.WriteUInt64(this.buffer, this.offset, value);
        }

        /// <summary>
        /// Appends the specified value to the buffer.
        /// </summary>
        /// <param name="str">Contains the character to append.</param>
        /// <param name="index">
        /// The index of the character in the string. This will be updated to
        /// point past the end of the current code point.
        /// </param>
        /// <param name="buffer">The buffer to output to.</param>
        /// <param name="offset">
        /// The index to start copying to. This will be updated to point to the
        /// end of the bytes written to the buffer.
        /// </param>
        /// <returns>The number of bytes written.</returns>
        internal static int AppendChar(string str, ref int index, byte[] buffer, int offset)
        {
            int ch = str[index];
            if (TryAppendChar(buffer, offset, ch))
            {
                return 1;
            }

            // Check if we're a surrogate pair
            if (ch >= 0xd800)
            {
                index++;
                if (index < str.Length)
                {
                    ch = char.ConvertToUtf32((char)ch, str[index]);
                }
            }

            return AppendUtf32(new byte[4], buffer, offset, ch);
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
            // CountDigits can't handle zero (there's a special case for it
            // in the WriteUInt64 method)
            int digits = (arrayIndex == 0) ? 1 : IntegerConverter.CountDigits((uint)arrayIndex);
            byte[] text = new byte[digits];
            IntegerConverter.WriteUInt64(text, 0, (uint)arrayIndex);

            this.keyParts.Add(text);
            this.keyLength += digits + 1;
        }

        private static int AppendUtf32(byte[] utf8Buffer, byte[] output, int offset, int utf32)
        {
            int bytes = JsonStringEncoding.AppendUtf32(utf32, utf8Buffer, 0);
            for (int i = 0; i < bytes; i++)
            {
                byte b = utf8Buffer[i];
                output[offset] = 0x25; // %
                output[offset + 1] = (byte)PrimitiveDigits.UppercaseHex[b >> 4];
                output[offset + 2] = (byte)PrimitiveDigits.UppercaseHex[b & 0xf];
                offset += 3;
            }

            return bytes * 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CopyBytes(byte[] source, byte[] destination, int destinationOffset)
        {
            Buffer.BlockCopy(source, 0, destination, destinationOffset, source.Length);
            return destinationOffset + source.Length;
        }

        private static bool InRange(int ch, uint lower, uint upper)
        {
            return ((uint)ch - lower) <= (upper - lower);
        }

        private static bool TryAppendChar(byte[] buffer, int offset, int ch)
        {
            // http://www.w3.org/TR/html5/forms.html#url-encoded-form-data
            // Happy path for ASCII letters/numbers
            if (InRange(ch, 0x30, 0x39) ||
                InRange(ch, 0x41, 0x5a) ||
                InRange(ch, 0x61, 0x7a))
            {
                buffer[offset] = (byte)ch;
                return true;
            }
            else
            {
                switch (ch)
                {
                    case 0x20:
                        buffer[offset] = 0x2b; // Replace spaces with +
                        return true;

                    case 0x2a:
                    case 0x2d:
                    case 0x2e:
                    case 0x5f:
                        buffer[offset] = (byte)ch; // Don't need escaping
                        return true;

                    default:
                        return false;
                }
            }
        }

        private void AppendAscii(string text)
        {
            this.EnsureBufferHasSpace(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                this.buffer[this.offset + i] = (byte)text[i];
            }

            this.offset += text.Length;
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
    }
}
