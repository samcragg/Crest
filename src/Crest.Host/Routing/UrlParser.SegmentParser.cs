// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    /// <content>
    /// Contains the nested helper <see cref="SegmentParser"/> class.
    /// </content>
    internal partial class UrlParser
    {
        // A capture group is surrounded by braces {} so to allow braces to be
        // used inside a literal capture they have to be doubled up (i.e.
        // 'braces{{}}' would be unescaped to 'braces{}'). This also allows us
        // to remove the braces in group captures (i.e. '{param}' -> 'param')
        private struct SegmentParser
        {
            private readonly char[] buffer;
            private readonly UrlParser parent;
            private int index;

            public SegmentParser(UrlParser parent, string segment, int start, int end)
                : this()
            {
                this.parent = parent;
                this.buffer = new char[end - start];

                for (int i = start; i < end;)
                {
                    char ch = segment[i];
                    i++;

                    if (ch == '{')
                    {
                        if (!this.OnOpenBrace(segment, start, end, ref i))
                        {
                            break;
                        }
                    }
                    else if (ch == '}')
                    {
                        if (!this.OnCloseBrace(segment, end, ref i))
                        {
                            break;
                        }
                    }
                    else
                    {
                        this.AppendToBuffer(ch);
                    }
                }
            }

            internal SegmentType Type { get; private set; }

            internal string Value => new string(this.buffer, 0, this.index);

            private void AppendToBuffer(char ch)
            {
                this.buffer[this.index] = ch;
                this.index++;
            }

            private bool OnCloseBrace(string segment, int end, ref int i)
            {
                // Is it escaped (note i points one past the current position)?
                if ((i < end) && (segment[i] == '}'))
                {
                    i++; // Skip it
                    this.AppendToBuffer('}');
                    return true;
                }

                if ((i == end) && (this.Type == SegmentType.PartialCapture))
                {
                    this.Type = SegmentType.Capture;
                    return true;
                }

                this.parent.OnError(ErrorType.UnescapedBrace, i - 1, 1, segment);
                this.Type = SegmentType.Error;
                return false;
            }

            private bool OnOpenBrace(string segment, int start, int end, ref int i)
            {
                // Is it escaped (note i points one past the current position)?
                if ((i < end) && (segment[i] == '{'))
                {
                    i++; // Skip it
                    this.AppendToBuffer('{');
                    return true;
                }

                if (i == (start + 1))
                {
                    this.Type = SegmentType.PartialCapture;
                    return true;
                }

                this.parent.OnError(ErrorType.UnescapedBrace, i - 1, 1, segment);
                this.Type = SegmentType.Error;
                return false;
            }
        }
    }
}
