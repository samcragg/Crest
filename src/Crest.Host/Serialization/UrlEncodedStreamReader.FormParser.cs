// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <content>
    /// Contains the nested <see cref="FormParser"/> struct.
    /// </content>
    internal sealed partial class UrlEncodedStreamReader
    {
        private struct FormParser
        {
            private StringBuffer buffer;
            private bool inValue;
            private string key;
            private List<Pair> pairs;

            public FormParser(StreamIterator iterator, StringBuffer buffer)
                : this()
            {
                this.buffer = buffer;
                this.key = string.Empty;
                this.pairs = new List<Pair>();

                while (iterator.MoveNext())
                {
                    this.AppendCharacter(iterator);
                }

                // Add the last pair. This also ensure that if the input stream
                // is empty we at least return one empty pair, making things
                // easier for the reader
                this.pairs.Add(new Pair(this.key, this.buffer.ToString()));

                // Sort the results so that nested properties can be read all
                // together (i.e. NestedClass.Property1 and NestedClass.Property2
                // can be read while building the NestedClass object)
                this.pairs.Sort();
            }

            public IReadOnlyList<Pair> Pairs => this.pairs;

            private static bool AddHexValue(char c, ref uint value)
            {
                uint digit = (uint)(c - '0');
                if (digit > 9)
                {
                    digit = (uint)(c - 'A');
                    if (digit > 5)
                    {
                        digit = (uint)(c - 'a');
                        if (digit > 5)
                        {
                            return false;
                        }
                    }

                    digit += 10;
                }

                value += digit;
                return true;
            }

            private static char DecodeHexPair(StreamIterator iterator)
            {
                if (iterator.MoveNext())
                {
                    char first = iterator.Current;
                    if (iterator.MoveNext())
                    {
                        uint value = 0;
                        if (!AddHexValue(first, ref value))
                        {
                            throw new FormatException("Invalid hexadecimal character");
                        }

                        value <<= 4;
                        if (!AddHexValue(iterator.Current, ref value))
                        {
                            throw new FormatException("Invalid hexadecimal character");
                        }

                        return (char)value;
                    }
                }

                throw new FormatException("Expected percent-encoded value");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AppendCharacter(StreamIterator iterator)
            {
                char c = iterator.Current;
                switch (c)
                {
                    case '&':
                        this.pairs.Add(new Pair(this.key, this.buffer.ToString()));

                        this.inValue = false;
                        this.key = string.Empty;
                        this.buffer.Clear();
                        break;

                    case '=':
                        if (this.inValue)
                        {
                            goto default;
                        }
                        else
                        {
                            this.inValue = true;
                            this.key = this.buffer.ToString();
                            this.buffer.Clear();
                        }

                        break;

                    case '+':
                        this.buffer.Append(' ');
                        break;

                    case '%':
                        this.buffer.Append(DecodeHexPair(iterator));
                        break;

                    default:
                        this.buffer.Append(c);
                        break;
                }
            }
        }
    }
}
