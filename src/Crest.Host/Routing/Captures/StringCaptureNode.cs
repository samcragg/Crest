// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Captures
{
    using System;

    /// <summary>
    /// Allows the capturing of string information from the route.
    /// </summary>
    internal sealed class StringCaptureNode : IMatchNode
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
        public NodeMatchInfo Match(ReadOnlySpan<char> text)
        {
            ReadOnlySpan<char> segment = GetPathSegment(text);
            return new NodeMatchInfo(
                segment.Length,
                this.ParameterName,
                segment.ToString());
        }

        /// <inheritdoc />
        public bool TryConvertValue(ReadOnlySpan<char> value, out object result)
        {
            result = value.ToString();
            return true;
        }

        /// <summary>
        /// Gets the current segment of the path.
        /// </summary>
        /// <param name="text">The string to match.</param>
        /// <returns>The current segment from the text.</returns>
        internal static ReadOnlySpan<char> GetPathSegment(in ReadOnlySpan<char> text)
        {
            int end = text.IndexOf('/');
            if (end < 0)
            {
                end = text.Length;
            }

            return text.Slice(0, end);
        }
    }
}
