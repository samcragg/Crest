// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Captures
{
    using System;

    /// <summary>
    /// Allows the capturing of globally unique identifier values from the route.
    /// </summary>
    internal sealed class GuidCaptureNode : IMatchNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GuidCaptureNode"/> class.
        /// </summary>
        /// <param name="parameter">
        /// The name of the parameter being captured.
        /// </param>
        public GuidCaptureNode(string parameter)
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
            var node = other as GuidCaptureNode;
            return string.Equals(this.ParameterName, node?.ParameterName, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public NodeMatchInfo Match(ReadOnlySpan<char> text)
        {
            int length = GetStringLength(ref text);
            if ((length > 0) && TryParseGuid(text, length, out Guid result))
            {
                return new NodeMatchInfo(length, this.ParameterName, result);
            }
            else
            {
                return NodeMatchInfo.None;
            }
        }

        /// <inheritdoc />
        public bool TryConvertValue(ReadOnlySpan<char> value, out object result)
        {
            int length = GetStringLength(ref value);
            if ((length > 0) && TryParseGuid(value, length, out Guid guid))
            {
                result = guid;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        private static int CheckHyphen(in ReadOnlySpan<char> str, bool hyphensAllowed, int index)
        {
            if (hyphensAllowed)
            {
                if (str[index] != '-')
                {
                    return int.MaxValue;
                }

                return index + 1;
            }
            else
            {
                return index;
            }
        }

        private static int GetStringLength(ref ReadOnlySpan<char> span)
        {
            // Check for brackets
            if (span.Length >= 38)
            {
                char begin = span[0];
                char end = span[37];
                if (((begin == '{') && (end == '}')) ||
                    ((begin == '(') && (end == ')')))
                {
                    span = span.Slice(1);
                    return 36;
                }
            }

            if (span.Length >= 32)
            {
                return (span[8] == '-') ? 36 : 32;
            }
            else
            {
                return -1;
            }
        }

        private static bool GetHexInt32(in ReadOnlySpan<char> str, int start, int end, ref int result)
        {
            // If the check to hyphen fails then it will set the start index to
            // out of bounds
            if (start > str.Length)
            {
                return false;
            }

            uint value = 0;
            for (int i = start; i < end; i++)
            {
                char c = str[i];
                uint digit = (uint)(c - '0');
                if (digit > 10)
                {
                    digit = (uint)(c - 'a');
                    if (digit > 6)
                    {
                        digit = (uint)(c - 'A');
                        if (digit > 6)
                        {
                            return false;
                        }
                    }

                    digit += 10u;
                }

                value = (value * 16u) + digit;
            }

            result = (int)value;
            return true;
        }

        private static bool TryParseGuid(in ReadOnlySpan<char> value, int length, out Guid guid)
        {
            guid = default;
            bool hyphensAllowed = length > 32;

            int index = 0;
            int a = 0;
            if (!GetHexInt32(value, index, index + 8, ref a))
            {
                return false;
            }

            index = CheckHyphen(value, hyphensAllowed, index + 8);

            int b = 0;
            if (!GetHexInt32(value, index, index + 4, ref b))
            {
                return false;
            }

            index = CheckHyphen(value, hyphensAllowed, index + 4);

            int c = 0;
            if (!GetHexInt32(value, index, index + 4, ref c))
            {
                return false;
            }

            index = CheckHyphen(value, hyphensAllowed, index + 4);

            int d = 0;
            if (!GetHexInt32(value, index, index + 4, ref d))
            {
                return false;
            }

            index = CheckHyphen(value, hyphensAllowed, index + 4);

            int e = 0;
            int f = 0;
            if (!GetHexInt32(value, index, index + 4, ref e) ||
                !GetHexInt32(value, index + 4, index + 12, ref f))
            {
                return false;
            }

            guid = new Guid(
                a,
                (short)b,
                (short)c,
                (byte)(d >> 8),
                (byte)d,
                (byte)(e >> 8),
                (byte)e,
                (byte)(f >> 24),
                (byte)(f >> 16),
                (byte)(f >> 8),
                (byte)f);
            return true;
        }
    }
}
