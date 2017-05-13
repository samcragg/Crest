// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
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
        public NodeMatchResult Match(StringSegment segment)
        {
            if (this.TryConvertValue(segment, out object result))
            {
                return new NodeMatchResult(this.ParameterName, result);
            }
            else
            {
                return NodeMatchResult.None;
            }
        }

        /// <inheritdoc />
        public bool TryConvertValue(StringSegment value, out object result)
        {
            int start = value.Start;
            if (CheckStringLength(value, ref start))
            {
                var guid = default(Guid);
                if (TryParseGuid(value.String, value.Count > 32, start, ref guid))
                {
                    result = guid;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private static int CheckHyphen(string str, bool hyphensAllowed, int index)
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

        private static bool CheckStringLength(StringSegment segment, ref int start)
        {
            switch (segment.Count)
            {
                case 32:
                case 36:
                    return true;

                case 38:
                    start++;
                    char first = segment[0];
                    if (first == '{')
                    {
                        return segment[37] == '}';
                    }
                    else if (first == '(')
                    {
                        return segment[37] == ')';
                    }
                    else
                    {
                        return false;
                    }

                default:
                    return false;
            }
        }

        private static bool GetHexInt32(string str, int start, int end, ref int result)
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

        private static bool TryParseGuid(string value, bool hyphensAllowed, int start, ref Guid guid)
        {
            int index = start;
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
