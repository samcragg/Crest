// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;

    /// <summary>
    /// Allows the capturing of string information from the route.
    /// </summary>
    internal sealed class StringCaptureNode : IMatchNode
    {
        private readonly string property;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringCaptureNode"/> class.
        /// </summary>
        /// <param name="property">
        /// The name of the property to capture the value to.
        /// </param>
        public StringCaptureNode(string property)
        {
            this.property = property;
        }

        /// <inheritdoc />
        public int Priority
        {
            get { return 100; }
        }

        /// <inheritdoc />
        public bool Equals(IMatchNode other)
        {
            var node = other as StringCaptureNode;
            return string.Equals(this.property, node?.property, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public NodeMatchResult Match(StringSegment segment)
        {
            return new NodeMatchResult(this.property, segment.ToString());
        }
    }
}
