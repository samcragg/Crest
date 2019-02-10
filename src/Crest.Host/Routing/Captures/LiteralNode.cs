// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Captures
{
    using System;
    using static System.Diagnostics.Debug;

    /// <summary>
    /// Matches a string literal in the route.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "Standard dictates we should normalize to lowercase")]
    internal sealed class LiteralNode : IMatchNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LiteralNode"/> class.
        /// </summary>
        /// <param name="literal">The string literal to match.</param>
        public LiteralNode(string literal)
        {
            this.Literal = literal.ToLowerInvariant();
        }

        /// <summary>
        /// Gets the literal that this node matches.
        /// </summary>
        public string Literal { get; }

        /// <inheritdoc />
        public int Priority => 1000;

        /// <inheritdoc />
        string IQueryValueConverter.ParameterName => throw new NotSupportedException();

        /// <inheritdoc />
        public bool Equals(IMatchNode other)
        {
            var node = other as LiteralNode;
            return string.Equals(this.Literal, node?.Literal, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public NodeMatchInfo Match(ReadOnlySpan<char> text)
        {
            if (StartsWith(text, this.Literal))
            {
                return new NodeMatchInfo(this.Literal.Length, null, null);
            }
            else
            {
                return NodeMatchInfo.None;
            }
        }

        /// <inheritdoc />
        bool IQueryValueConverter.TryConvertValue(ReadOnlySpan<char> value, out object result)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Determines whether the specified span of text starts with the
        /// specified text, ignoring case.
        /// </summary>
        /// <param name="span">The span to check.</param>
        /// <param name="text">The starting text (must be lowercase).</param>
        /// <returns>
        /// <c>true</c> if the span starts with the specified text; otherwise,
        /// <c>false</c>.
        /// </returns>
        internal static bool StartsWith(in ReadOnlySpan<char> span, string text)
        {
            Assert(text.Equals(text.ToLowerInvariant(), StringComparison.Ordinal), "Passed in text must be lowercase");

            if (text.Length > span.Length)
            {
                return false;
            }

            for (int i = 0; i < text.Length; i++)
            {
                char c = span[i];
                if ((uint)(c - 'A') <= ('Z' - 'A'))
                {
                    c = (char)(c + ('a' - 'A'));
                }

                if (text[i] != c)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
