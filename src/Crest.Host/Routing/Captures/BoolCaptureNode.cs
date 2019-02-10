// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Captures
{
    using System;

    /// <summary>
    /// Allows the capturing of boolean values from the route.
    /// </summary>
    internal sealed class BoolCaptureNode : IMatchNode
    {
        private static readonly object BoxedFalse = false;
        private static readonly object BoxedTrue = true;
        private static readonly string[] FalseValues = { "false", "0" };
        private static readonly string[] TrueValues = { "true", "1" };

        /// <summary>
        /// Initializes a new instance of the <see cref="BoolCaptureNode"/> class.
        /// </summary>
        /// <param name="parameter">
        /// The name of the parameter being captured.
        /// </param>
        public BoolCaptureNode(string parameter)
        {
            this.ParameterName = parameter;
        }

        /// <inheritdoc />
        public string ParameterName { get; }

        /// <inheritdoc />
        public int Priority => 500;

        /// <inheritdoc />
        public bool Equals(IMatchNode other)
        {
            var node = other as BoolCaptureNode;
            return string.Equals(this.ParameterName, node?.ParameterName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public NodeMatchInfo Match(ReadOnlySpan<char> text)
        {
            object converted = ParseValue(text, out int length);
            if (converted == null)
            {
                return NodeMatchInfo.None;
            }
            else
            {
                return new NodeMatchInfo(length, this.ParameterName, converted);
            }
        }

        /// <inheritdoc />
        public bool TryConvertValue(ReadOnlySpan<char> value, out object result)
        {
            if (value.Length == 0)
            {
                result = BoxedTrue;
                return true;
            }
            else
            {
                result = ParseValue(value, out _);
                return result != null;
            }
        }

        private static int Matches(in ReadOnlySpan<char> span, string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (LiteralNode.StartsWith(span, values[i]))
                {
                    return values[i].Length;
                }
            }

            return -1;
        }

        private static object ParseValue(in ReadOnlySpan<char> value, out int length)
        {
            length = Matches(value, FalseValues);
            if (length < 0)
            {
                length = Matches(value, TrueValues);
                if (length < 0)
                {
                    return null;
                }

                return BoxedTrue;
            }

            return BoxedFalse;
        }
    }
}
