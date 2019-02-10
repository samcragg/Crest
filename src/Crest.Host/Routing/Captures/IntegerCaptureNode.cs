﻿// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Captures
{
    using System;
    using Crest.Host.Conversion;
    using static System.Diagnostics.Debug;

    /// <summary>
    /// Allows the capturing of integer values from the route.
    /// </summary>
    internal sealed class IntegerCaptureNode : IMatchNode
    {
        private readonly IntegerType type;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegerCaptureNode"/> class.
        /// </summary>
        /// <param name="parameter">
        /// The name of the parameter being captured.
        /// </param>
        /// <param name="targetType">The type of the integer.</param>
        public IntegerCaptureNode(string parameter, Type targetType)
        {
            this.ParameterName = parameter;
            this.type = GetIntegerType(targetType);
        }

        // Names MUST match the name of the structs in the System namespace
        private enum IntegerType
        {
            Byte,
            Int16,
            Int32,
            Int64,
            SByte,
            UInt16,
            UInt32,
            UInt64,
        }

        /// <inheritdoc />
        public string ParameterName { get; }

        /// <inheritdoc />
        public int Priority => 500;

        /// <inheritdoc />
        public bool Equals(IMatchNode other)
        {
            if (other is IntegerCaptureNode node)
            {
                return (this.type == node.type) &&
                        string.Equals(this.ParameterName, node.ParameterName, StringComparison.Ordinal);
            }

            return false;
        }

        /// <inheritdoc />
        public NodeMatchInfo Match(ReadOnlySpan<char> text)
        {
            ParseResult<long> parseResult = IntegerConverter.TryReadSignedInt(
                text,
                long.MinValue,
                long.MaxValue);

            if (parseResult.IsSuccess)
            {
                return new NodeMatchInfo(
                    parseResult.Length,
                    this.ParameterName,
                    this.BoxInteger(parseResult.Value));
            }
            else
            {
                return NodeMatchInfo.None;
            }
        }

        /// <inheritdoc />
        public bool TryConvertValue(ReadOnlySpan<char> value, out object result)
        {
            ParseResult<long> parseResult = IntegerConverter.TryReadSignedInt(
                value,
                long.MinValue,
                long.MaxValue);

            if (parseResult.IsSuccess)
            {
                result = this.BoxInteger(parseResult.Value);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        private static IntegerType GetIntegerType(Type type)
        {
            if (string.Equals("System", type.Namespace, StringComparison.Ordinal) &&
                Enum.TryParse(type.Name, out IntegerType integerType))
            {
                return integerType;
            }

            throw new ArgumentException("Unknown integer type {0}", type.FullName);
        }

        private object BoxInteger(long value)
        {
            // We need to box it as the correct type as unboxing will not do the
            // conversion for us (i.e. object b = 1; short goesBang = (short)b;)
            switch (this.type)
            {
                case IntegerType.Byte:
                    return (byte)value;

                case IntegerType.Int16:
                    return (short)value;

                case IntegerType.Int32:
                    return (int)value;

                case IntegerType.Int64:
                    return value;

                case IntegerType.SByte:
                    return (sbyte)value;

                case IntegerType.UInt16:
                    return (ushort)value;

                case IntegerType.UInt32:
                    return (uint)value;

                default:
                    Assert(this.type == IntegerType.UInt64, "Unexpected value");
                    return (ulong)value;
            }
        }
    }
}
