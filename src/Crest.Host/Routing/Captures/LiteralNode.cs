// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Captures
{
    using System;

    /// <summary>
    /// Matches a string literal in the route.
    /// </summary>
    internal sealed class LiteralNode : IMatchNode
    {
        private static readonly NodeMatchResult Success = new NodeMatchResult(string.Empty, null);

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteralNode"/> class.
        /// </summary>
        /// <param name="literal">The string literal to match.</param>
        public LiteralNode(string literal)
        {
            this.Literal = literal;
        }

        /// <summary>
        /// Gets the literal that this node matches.
        /// </summary>
        public string Literal
        {
            get;
        }

        /// <inheritdoc />
        public int Priority => 1000;

        /// <inheritdoc />
        string IQueryValueConverter.ParameterName => throw new NotSupportedException();

        /// <inheritdoc />
        public bool Equals(IMatchNode other)
        {
            var node = other as LiteralNode;
            return string.Equals(this.Literal, node?.Literal, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public NodeMatchResult Match(ReadOnlySpan<char> segment)
        {
            if (segment.Equals(this.Literal.AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                return Success;
            }
            else
            {
                return NodeMatchResult.None;
            }
        }

        /// <inheritdoc />
        bool IQueryValueConverter.TryConvertValue(ReadOnlySpan<char> value, out object result)
        {
            throw new NotSupportedException();
        }
    }
}
