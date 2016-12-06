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
    internal sealed class BoolCaptureNode : IMatchNode
    {
        private static readonly object BoxedFalse = false;
        private static readonly object BoxedTrue = true;
        private static readonly string[] FalseValues = { "false", "0" };
        private static readonly string[] TrueValues = { "true", "1" };
        private readonly string property;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoolCaptureNode"/> class.
        /// </summary>
        /// <param name="property">
        /// The name of the property to capture the value to.
        /// </param>
        public BoolCaptureNode(string property)
        {
            this.property = property;
        }

        /// <inheritdoc />
        public int Priority
        {
            get { return 500; }
        }

        /// <inheritdoc />
        public bool Equals(IMatchNode other)
        {
            var node = other as BoolCaptureNode;
            return string.Equals(this.property, node?.property, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public NodeMatchResult Match(StringSegment segment)
        {
            if (Matches(segment, FalseValues))
            {
                return new NodeMatchResult(this.property, BoxedFalse);
            }
            else if (Matches(segment, TrueValues))
            {
                return new NodeMatchResult(this.property, BoxedTrue);
            }
            else
            {
                return NodeMatchResult.None;
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
    }
}
