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
    internal sealed class StringCaptureNode : IMatchNode, IQueryValueConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringCaptureNode"/> class.
        /// </summary>
        /// <param name="parameter">
        /// The name of the parameter being captured.
        /// </param>
        public StringCaptureNode(string parameter)
        {
            this.ParameterName = parameter;
        }

        /// <inheritdoc />
        public string ParameterName { get; }

        /// <inheritdoc />
        public int Priority => 100;

        /// <inheritdoc />
        public bool Equals(IMatchNode other)
        {
            var node = other as StringCaptureNode;
            return string.Equals(this.ParameterName, node?.ParameterName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public NodeMatchResult Match(StringSegment segment)
        {
            return new NodeMatchResult(this.ParameterName, segment.ToString());
        }

        /// <inheritdoc />
        public bool TryConvertValue(StringSegment value, out object result)
        {
            result = value.ToString();
            return true;
        }
    }
}
