// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Util
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq.Expressions;
    using System.Reflection;
    using Crest.Host.Conversion;
    using Crest.Host.Serialization.UrlEncoded;
    using SCM = System.ComponentModel;

    /// <summary>
    /// Provides methods for converting values to form part of a URL.
    /// </summary>
    internal static class UrlValueConverter
    {
        private static readonly MethodInfo AppendObjectMethod = GetMethod(AppendObject);

        private static readonly IReadOnlyDictionary<Type, MethodInfo> KnownConverters =
            new Dictionary<Type, MethodInfo>(18)
            {
                { typeof(bool), GetMethod(AppendBool) },
                { typeof(byte), GetMethod(AppendByte) },
                { typeof(char), GetMethod(AppendChar) },
                { typeof(DateTime), GetMethod(AppendDateTime) },
                { typeof(decimal), GetMethod(AppendDecimal) },
                { typeof(double), GetMethod(AppendDouble) },
                { typeof(float), GetMethod(AppendSingle) },
                { typeof(Guid), GetMethod(AppendGuid) },
                { typeof(int), GetMethod(AppendInt32) },
                { typeof(long), GetMethod(AppendInt64) },
                { typeof(sbyte), GetMethod(AppendSByte) },
                { typeof(short), GetMethod(AppendInt16) },
                { typeof(string), GetMethod(AppendString) },
                { typeof(TimeSpan), GetMethod(AppendTimeSpan) },
                { typeof(uint), GetMethod(AppendUInt32) },
                { typeof(ulong), GetMethod(AppendUInt64) },
                { typeof(ushort), GetMethod(AppendUInt16) },
                { typeof(Uri), GetMethod(AppendUri) },
            };

        /// <summary>
        /// Appends the string value to the buffer, escaping the characters as
        /// required.
        /// </summary>
        /// <param name="buffer">The buffer to copy the string to.</param>
        /// <param name="value">The string value to escape.</param>
        public static void AppendEscapedString(StringBuffer buffer, string value)
        {
            Span<byte> bytes = stackalloc byte[UrlStringEncoding.MaxBytesPerCharacter];
            for (int index = 0; index < value.Length; index++)
            {
                int count = UrlStringEncoding.AppendChar(
                    UrlStringEncoding.SpaceOption.Percent,
                    value,
                    ref index,
                    bytes);

                buffer.AppendAscii(bytes.Slice(0, count));
            }
        }

        /// <summary>
        /// Creates an expression that will append a value of the specified
        /// type to the specified string buffer.
        /// </summary>
        /// <param name="buffer">
        /// The <see cref="StringBuffer"/> to append on.
        /// </param>
        /// <param name="objectArray">
        /// An object array that contains the value to convert.
        /// </param>
        /// <param name="index">
        /// The index in the array of the value to convert.
        /// </param>
        /// <param name="type">The type of the value in the array.</param>
        /// <returns>An expression that appends the value.</returns>
        public static Expression AppendValue(
            ParameterExpression buffer,
            ParameterExpression objectArray,
            int index,
            Type type)
        {
            if (!KnownConverters.TryGetValue(type, out MethodInfo method))
            {
                method = AppendObjectMethod;
            }

            return Expression.Call(
                method,
                buffer,
                Expression.ArrayIndex(objectArray, Expression.Constant(index)));
        }

        private static void AppendBool(StringBuffer buffer, object value)
        {
            buffer.Append((bool)value ? "true" : "false");
        }

        private static void AppendByte(StringBuffer buffer, object value)
        {
            AppendUnsignedInteger(buffer, (byte)value);
        }

        private static void AppendChar(StringBuffer buffer, object value)
        {
            Span<byte> bytes = stackalloc byte[UrlStringEncoding.MaxBytesPerCharacter];
            int count = UrlStringEncoding.AppendChar(
                UrlStringEncoding.SpaceOption.Percent,
                (char)value,
                bytes);

            buffer.AppendAscii(bytes.Slice(0, count));
        }

        private static void AppendDateTime(StringBuffer buffer, object value)
        {
            Span<byte> bytes = stackalloc byte[DateTimeConverter.MaximumTextLength];
            int length = DateTimeConverter.WriteDateTime(bytes, (DateTime)value);
            buffer.AppendAscii(bytes.Slice(0, length));
        }

        private static void AppendDecimal(StringBuffer buffer, object value)
        {
            buffer.Append(((decimal)value).ToString(NumberFormatInfo.InvariantInfo));
        }

        private static void AppendDouble(StringBuffer buffer, object value)
        {
            buffer.Append(((double)value).ToString(NumberFormatInfo.InvariantInfo));
        }

        private static void AppendGuid(StringBuffer buffer, object value)
        {
            Span<byte> bytes = stackalloc byte[GuidConverter.MaximumTextLength];
            int length = GuidConverter.WriteGuid(bytes, (Guid)value);
            buffer.AppendAscii(bytes.Slice(0, length));
        }

        private static void AppendInt16(StringBuffer buffer, object value)
        {
            AppendSignedInteger(buffer, (short)value);
        }

        private static void AppendInt32(StringBuffer buffer, object value)
        {
            AppendSignedInteger(buffer, (int)value);
        }

        private static void AppendInt64(StringBuffer buffer, object value)
        {
            AppendSignedInteger(buffer, (long)value);
        }

        private static void AppendObject(StringBuffer buffer, object value)
        {
            SCM.TypeConverter converter = SCM.TypeDescriptor.GetConverter(value);
            string converted = converter.ConvertToInvariantString(value);
            AppendEscapedString(buffer, converted);
        }

        private static void AppendSByte(StringBuffer buffer, object value)
        {
            AppendSignedInteger(buffer, (sbyte)value);
        }

        private static void AppendSignedInteger(StringBuffer buffer, long value)
        {
            Span<byte> bytes = stackalloc byte[IntegerConverter.MaximumTextLength];
            int length = IntegerConverter.WriteInt64(bytes, value);
            buffer.AppendAscii(bytes.Slice(0, length));
        }

        private static void AppendSingle(StringBuffer buffer, object value)
        {
            buffer.Append(((float)value).ToString(NumberFormatInfo.InvariantInfo));
        }

        private static void AppendString(StringBuffer buffer, object value)
        {
            AppendEscapedString(buffer, (string)value);
        }

        private static void AppendTimeSpan(StringBuffer buffer, object value)
        {
            Span<byte> bytes = stackalloc byte[TimeSpanConverter.MaximumTextLength];
            int length = TimeSpanConverter.WriteTimeSpan(bytes, (TimeSpan)value);
            buffer.AppendAscii(bytes.Slice(0, length));
        }

        private static void AppendUInt16(StringBuffer buffer, object value)
        {
            AppendUnsignedInteger(buffer, (ushort)value);
        }

        private static void AppendUInt32(StringBuffer buffer, object value)
        {
            AppendUnsignedInteger(buffer, (uint)value);
        }

        private static void AppendUInt64(StringBuffer buffer, object value)
        {
            AppendUnsignedInteger(buffer, (ulong)value);
        }

        private static void AppendUnsignedInteger(StringBuffer buffer, ulong value)
        {
            Span<byte> bytes = stackalloc byte[IntegerConverter.MaximumTextLength];
            int length = IntegerConverter.WriteUInt64(bytes, value);
            buffer.AppendAscii(bytes.Slice(0, length));
        }

        private static void AppendUri(StringBuffer buffer, object value)
        {
            var uriValue = (Uri)value;
            AppendEscapedString(
                buffer,
                uriValue.IsAbsoluteUri ? uriValue.AbsoluteUri : uriValue.OriginalString);
        }

        private static MethodInfo GetMethod(Action<StringBuffer, object> action)
        {
            return action.Method;
        }
    }
}
