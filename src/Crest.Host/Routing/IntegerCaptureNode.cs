// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using Crest.Host.Conversion;

    /// <summary>
    /// Allows the capturing of integer values from the route.
    /// </summary>
    internal sealed class IntegerCaptureNode : IMatchNode
    {
        private readonly string property;
        private readonly IntegerType type;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegerCaptureNode"/> class.
        /// </summary>
        /// <param name="property">
        /// The name of the property to capture the value to.
        /// </param>
        /// <param name="targetType">The type of the integer.</param>
        public IntegerCaptureNode(string property, Type targetType)
        {
            this.property = property;
            this.type = GetIntegerType(targetType);
        }

        private enum IntegerType
        {
            Byte,
            Int16,
            Int32,
            Int64,
            SByte,
            UInt16,
            UInt32,
            UInt64
        }

        /// <inheritdoc />
        public int Priority
        {
            get { return 500; }
        }

        /// <inheritdoc />
        public bool Equals(IMatchNode other)
        {
            var node = other as IntegerCaptureNode;
            if (node == null)
            {
                return false;
            }

            return (this.type == node.type) &&
                    string.Equals(this.property, node.property, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public NodeMatchResult Match(StringSegment segment)
        {
            long value;
            if (IntegerConverter.ParseSignedValue(segment, out value))
            {
                return new NodeMatchResult(this.property, this.BoxInteger(value));
            }
            else
            {
                return NodeMatchResult.None;
            }
        }

        private static IntegerType GetIntegerType(Type type)
        {
            if (string.Equals("System", type.Namespace, StringComparison.Ordinal))
            {
                IntegerType integerType;
                if (Enum.TryParse(type.Name, out integerType))
                {
                    return integerType;
                }
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

                case IntegerType.UInt64:
                default:
                    System.Diagnostics.Debug.Assert(this.type == IntegerType.UInt64, "Unexpected value");
                    return (ulong)value;
            }
        }
    }
}
