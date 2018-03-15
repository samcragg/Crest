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
    /// Reads values from the underlying stream.
    /// </summary>
    public abstract class ValueReader
    {
        /// <summary>
        /// Reads a boolean from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        public abstract bool ReadBoolean();

        /// <summary>
        /// Read an 8-bit unsigned integer from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        public virtual byte ReadByte()
        {
            return (byte)this.ReadUnsignedInt(byte.MaxValue);
        }

        /// <summary>
        /// Reads a Unicode character from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        public abstract char ReadChar();

        /// <summary>
        /// Reads a <see cref="DateTime"/> from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        public virtual DateTime ReadDateTime()
        {
            ParseResult<DateTime> parsed =
                DateTimeConverter.TryReadDateTime(this.ReadTrimmedString());

            return this.ProcessResult(parsed, "date/time");
        }

        /// <summary>
        /// Reads a <see cref="decimal"/> from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        public virtual decimal ReadDecimal()
        {
            ParseResult<decimal> parsed =
                DecimalConverter.TryReadDecimal(this.ReadTrimmedString());

            return this.ProcessResult(parsed, "decimal");
        }

        /// <summary>
        /// Reads a double-precision floating-point number from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        public virtual double ReadDouble()
        {
            ParseResult<double> parsed =
                DoubleConverter.TryReadDouble(this.ReadTrimmedString());

            return this.ProcessResult(parsed, "double");
        }

        /// <summary>
        /// Reads a GUID from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        public virtual Guid ReadGuid()
        {
            ParseResult<Guid> parsed =
                GuidConverter.TryReadGuid(this.ReadTrimmedString());

            return this.ProcessResult(parsed, "GUID");
        }

        /// <summary>
        /// Reads a 16-bit signed integer from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        public virtual short ReadInt16()
        {
            return (short)this.ReadSignedInt(short.MinValue, short.MaxValue);
        }

        /// <summary>
        /// Reads a 32-bit signed integer from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        public virtual int ReadInt32()
        {
            return (int)this.ReadSignedInt(int.MinValue, int.MaxValue);
        }

        /// <summary>
        /// Reads a 64-bit signed integer from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        public virtual long ReadInt64()
        {
            return this.ReadSignedInt(long.MinValue, long.MaxValue);
        }

        /// <summary>
        /// Attempts to read a token from the stream that indicates there is no
        /// value.
        /// </summary>
        /// <returns>
        /// <c>true</c> if there exists no value to read; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool ReadNull();

        /// <summary>
        /// Reads an object from the stream.
        /// </summary>
        /// <param name="type">The type of the object to read.</param>
        /// <returns>The value read from the stream.</returns>
        public virtual object ReadObject(Type type)
        {
            string value = this.ReadString();

            SCM.TypeConverter converter = SCM.TypeDescriptor.GetConverter(type);
            return converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
        }

        /// <summary>
        /// Reads an 8-bit signed integer from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        [CLSCompliant(false)]
        public virtual sbyte ReadSByte()
        {
            return (sbyte)this.ReadSignedInt(sbyte.MinValue, sbyte.MaxValue);
        }

        /// <summary>
        /// Reads a single-precision floating-point number from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        public virtual float ReadSingle()
        {
            return (float)this.ReadDouble();
        }

        /// <summary>
        /// Reads a string of characters from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        public abstract string ReadString();

        /// <summary>
        /// Reads a <see cref="TimeSpan"/> from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        public virtual TimeSpan ReadTimeSpan()
        {
            ParseResult<TimeSpan> parsed =
                TimeSpanConverter.TryReadTimeSpan(this.ReadTrimmedString());

            return this.ProcessResult(parsed, "timespan");
        }

        /// <summary>
        /// Reads a 16-bit unsigned integer from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        [CLSCompliant(false)]
        public virtual ushort ReadUInt16()
        {
            return (ushort)this.ReadUnsignedInt(ushort.MaxValue);
        }

        /// <summary>
        /// Reads a 32-bit unsigned integer from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        [CLSCompliant(false)]
        public virtual uint ReadUInt32()
        {
            return (uint)this.ReadUnsignedInt(uint.MaxValue);
        }

        /// <summary>
        /// Reads a 64-bit unsigned integer from the stream.
        /// </summary>
        /// <returns>The value read from the stream.</returns>
        [CLSCompliant(false)]
        public virtual ulong ReadUInt64()
        {
            return this.ReadUnsignedInt(ulong.MaxValue);
        }

        /// <summary>
        /// Gets the current position in the stream, used for reporting errors.
        /// </summary>
        /// <returns>A displayable string representing the location.</returns>
        internal abstract string GetCurrentPosition();

        /// <summary>
        /// Reads a signed integer.
        /// </summary>
        /// <param name="min">The minimum allowed value for the integer.</param>
        /// <param name="max">The maximum allowed value for the integer.</param>
        /// <returns>A signed integer.</returns>
        protected virtual long ReadSignedInt(long min, long max)
        {
            ParseResult<long> result = IntegerConverter.TryReadSignedInt(
                this.ReadTrimmedString(),
                min,
                max);

            return this.ProcessResult(result, "integer");
        }

        /// <summary>
        /// Reads an unsigned integer.
        /// </summary>
        /// <param name="max">The maximum allowed value for the integer.</param>
        /// <returns>An unsigned integer.</returns>
        [CLSCompliant(false)]
        protected virtual ulong ReadUnsignedInt(ulong max)
        {
            ParseResult<ulong> result = IntegerConverter.TryReadUnsignedInt(
                this.ReadTrimmedString(),
                max);

            return this.ProcessResult(result, "integer");
        }

        /// <summary>
        /// Raises an exception if the result of the parsing operation returned
        /// an error; otherwise, the returns the parsed result.
        /// </summary>
        /// <typeparam name="T">The type of the result.</typeparam>
        /// <param name="result">The result of the parsing operation.</param>
        /// <param name="type">The display name of the type being parsed.</param>
        /// <returns>The parsed value.</returns>
        private protected virtual T ProcessResult<T>(ParseResult<T> result, string type)
        {
            if (!result.IsSuccess)
            {
                throw new FormatException($"Unable to read {type} at {this.GetCurrentPosition()}: '{result.Error}'.");
            }

            return result.Value;
        }

        /// <summary>
        /// Reads a trimmed string from the stream.
        /// </summary>
        /// <returns>A span containing the string.</returns>
        private protected abstract ReadOnlySpan<char> ReadTrimmedString();
    }
}
