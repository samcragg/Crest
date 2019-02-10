// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Captures
{
    using System;
    using Crest.Host.Conversion;

    /// <summary>
    /// Allows the capturing of information from the route and converting it
    /// with the types default TypeConverter.
    /// </summary>
    internal class VersionCaptureNode : IMatchNode
    {
        /// <summary>
        /// Gets the key that stores the version information.
        /// </summary>
        internal const string KeyName = "__version__";

        /// <inheritdoc />
        public int Priority => 1;

        /// <inheritdoc />
        string IQueryValueConverter.ParameterName => throw new NotSupportedException();

        /// <inheritdoc />
        public bool Equals(IMatchNode other)
        {
            return other is VersionCaptureNode;
        }

        /// <inheritdoc />
        public NodeMatchInfo Match(ReadOnlySpan<char> text)
        {
            if (text.Length > 1)
            {
                char v = text[0];
                if ((v == 'v') || (v == 'V'))
                {
                    ParseResult<long> result = IntegerConverter.TryReadSignedInt(
                        text.Slice(1),
                        int.MinValue,
                        int.MaxValue);

                    if (result.IsSuccess)
                    {
                        return new NodeMatchInfo(result.Length + 1, KeyName, (int)result.Value);
                    }
                }
            }

            return NodeMatchInfo.None;
        }

        /// <inheritdoc />
        bool IQueryValueConverter.TryConvertValue(ReadOnlySpan<char> value, out object result)
        {
            throw new NotSupportedException();
        }
    }
}
