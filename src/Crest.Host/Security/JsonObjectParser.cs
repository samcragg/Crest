// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using System;
    using System.Collections.Generic;
    using Crest.Host.Serialization;

    /// <summary>
    /// Allows the parsing of key/value pairs from JSON data.
    /// </summary>
    internal sealed partial class JsonObjectParser : IDisposable
    {
        private readonly ICharIterator iterator;
        private readonly StringBuffer stringBuffer = new StringBuffer();

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObjectParser"/> class.
        /// </summary>
        /// <param name="bytes">Contains the data to parse.</param>
        public JsonObjectParser(byte[] bytes)
        {
            this.iterator = new Utf8Enumerator(bytes);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObjectParser"/> class.
        /// </summary>
        /// <param name="json">Contains the data to parse.</param>
        public JsonObjectParser(string json)
        {
            this.iterator = new StringIterator(json);
        }

        /// <summary>
        /// Releases the resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            this.stringBuffer.Dispose();
        }

        /// <summary>
        /// Gets the key/value pairs from the JSON object.
        /// </summary>
        /// <returns>A sequence of key/value pairs.</returns>
        public IEnumerable<KeyValuePair<string, string>> GetPairs()
        {
            this.SkipWhiteSpace();
            this.Expect('{');

            // Special check for empty objects
            this.SkipWhiteSpace();
            if (this.iterator.Current == '}')
            {
                yield break;
            }

            do
            {
                this.SkipWhiteSpace();

                this.stringBuffer.Clear();
                this.ReadString();
                string key = this.stringBuffer.ToString();

                this.SkipWhiteSpace();
                this.Expect(':');
                this.SkipWhiteSpace();

                string value = this.ReadValue();

                yield return new KeyValuePair<string, string>(key, value);
                this.SkipWhiteSpace();
            }
            while (this.TryRead(','));

            this.Expect('}');
        }

        /// <summary>
        /// Gets the values from an array as strings.
        /// </summary>
        /// <returns>A sequence of strings representing the values in the array.</returns>
        public IEnumerable<string> GetArrayValues()
        {
            this.SkipWhiteSpace();
            this.Expect('[');

            // Special check for empty objects
            this.SkipWhiteSpace();
            if (this.iterator.Current == ']')
            {
                yield break;
            }

            do
            {
                this.SkipWhiteSpace();
                yield return this.ReadValue();

                this.SkipWhiteSpace();
            }
            while (this.TryRead(','));

            this.Expect(']');
        }

        private static bool IsWhiteSpace(char b)
        {
            return (b == ' ') || (b == '\t') || (b == '\n') || (b == '\r');
        }

        private void Expect(char c)
        {
            if (this.iterator.Current != c)
            {
                throw new FormatException($"Expected a '{c}' at {this.iterator.Position}.");
            }

            this.iterator.MoveNext();
        }

        private void ReadString()
        {
            int start = this.iterator.Position;
            this.Expect('"');
            do
            {
                if (this.iterator.Current == '"')
                {
                    this.iterator.MoveNext();
                    return;
                }

                this.stringBuffer.Append(JsonStringEncoding.DecodeChar(this.iterator));
            }
            while (this.iterator.MoveNext());

            // We reached the end of the stream without finding a closing
            // quotation mark :(
            throw new FormatException(
                $"Missing closing double quotes for string starting at {start}.");
        }

        private string ReadToken()
        {
            do
            {
                char c = this.iterator.Current;
                if ((c == ',') || (c == '}') || (c == ']'))
                {
                    break;
                }

                this.stringBuffer.Append(c);
            }
            while (this.iterator.MoveNext());

            string value = this.stringBuffer.ToString();
            if (string.Equals(value, "null", StringComparison.Ordinal))
            {
                return null;
            }
            else
            {
                return value;
            }
        }

        private string ReadValue()
        {
            this.stringBuffer.Clear();

            char c = this.iterator.Current;
            if (c == '"')
            {
                this.ReadString();
            }
            else if (c == '[')
            {
                this.SkipToEnd(']');
                this.iterator.MoveNext();
            }
            else if (c == '{')
            {
                this.SkipToEnd('}');
                this.iterator.MoveNext();
            }
            else
            {
                return this.ReadToken();
            }

            return this.stringBuffer.ToString();
        }

        private void SkipString()
        {
            int start = this.iterator.Position;
            this.stringBuffer.Append('"');

            bool inEscape = false;
            while (this.iterator.MoveNext())
            {
                char c = this.iterator.Current;
                this.stringBuffer.Append(c);
                if (inEscape)
                {
                    inEscape = false;
                }
                else
                {
                    if (c == '"')
                    {
                        return;
                    }
                    else if (c == '\\')
                    {
                        inEscape = true;
                    }
                }
            }

            throw new FormatException(
                $"Missing closing double quotes for string starting at {start}.");
        }

        private void SkipToEnd(char end)
        {
            int start = this.iterator.Position;
            this.stringBuffer.Append(this.iterator.Current);

            while (this.iterator.MoveNext())
            {
                char c = this.iterator.Current;
                if (c == end)
                {
                    this.stringBuffer.Append(c);
                    return;
                }
                else if (c == '"')
                {
                    this.SkipString();
                }
                else if (c == '{')
                {
                    this.SkipToEnd('}');
                }
                else if (c == '[')
                {
                    this.SkipToEnd(']');
                }
                else
                {
                    this.stringBuffer.Append(c);
                }
            }

            throw new FormatException(
                $"Missing '{end}' character for token started at {start}.");
        }

        private void SkipWhiteSpace()
        {
            while (IsWhiteSpace(this.iterator.Current))
            {
                // We rely on the fact that when the iterator moves to the end
                // it clears the Current property, so the above check will fail
                this.iterator.MoveNext();
            }
        }

        private bool TryRead(char c)
        {
            if (this.iterator.Current == c)
            {
                this.iterator.MoveNext();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
