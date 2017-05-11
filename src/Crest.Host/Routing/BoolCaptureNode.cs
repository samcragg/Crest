// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;

    /// <summary>
    /// Allows the capturing of boolean values from the route.
    /// </summary>
    internal sealed class BoolCaptureNode : IMatchNode, IQueryValueConverter
    {
        private static readonly object BoxedFalse = false;
        private static readonly object BoxedTrue = true;
        private static readonly string[] FalseValues = { "false", "0" };
        private static readonly string[] TrueValues = { "true", "1" };

        /// <summary>
        /// Initializes a new instance of the <see cref="BoolCaptureNode"/> class.
        /// </summary>
        /// <param name="parameter">
        /// The name of the property to capture the value to.
        /// </param>
        public BoolCaptureNode(string parameter)
        {
            this.ParameterName = parameter;
        }

        /// <inheritdoc />
        public int Priority => 500;

        /// <inheritdoc />
        public string ParameterName { get; }

        /// <inheritdoc />
        public bool Equals(IMatchNode other)
        {
            var node = other as BoolCaptureNode;
            return string.Equals(this.ParameterName, node?.ParameterName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public NodeMatchResult Match(StringSegment segment)
        {
            object converted = ParseValue(segment);
            if (converted == null)
            {
                return NodeMatchResult.None;
            }
            else
            {
                return new NodeMatchResult(this.ParameterName, converted);
            }
        }

        /// <inheritdoc />
        public bool TryConvertValue(StringSegment value, out object result)
        {
            if (value.Count == 0)
            {
                result = BoxedTrue;
                return true;
            }
            else
            {
                result = ParseValue(value);
                return result != null;
            }
        }

        private static bool Matches(StringSegment segment, string[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (segment.Equals(values[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static object ParseValue(StringSegment value)
        {
            if (Matches(value, FalseValues))
            {
                return BoxedFalse;
            }
            else if (Matches(value, TrueValues))
            {
                return BoxedTrue;
            }
            else
            {
                return null;
            }
        }
    }
}
