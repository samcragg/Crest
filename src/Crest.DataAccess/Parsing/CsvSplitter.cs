// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Splits a comma separated single line value per RFC 4180.
    /// </summary>
    internal class CsvSplitter
    {
        private readonly Lazy<StringBuilder> stringBuilder = new Lazy<StringBuilder>();

        /// <summary>
        /// Splits the specified single line value.
        /// </summary>
        /// <param name="value">The value to split.</param>
        /// <returns>The fields represented by <c>value</c>.</returns>
        public IEnumerable<string> Split(string value)
        {
            int index = 0;
            while (index < value.Length)
            {
                if (value[index] == '"')
                {
                    yield return this.UnescapeField(value, ref index);
                }
                else
                {
                    int end = value.IndexOf(',', index);
                    if (end < 0)
                    {
                        end = value.Length;
                    }

                    yield return value.Substring(index, end - index);
                    index = end + 1;
                }
            }
        }

        private string UnescapeField(string value, ref int index)
        {
            StringBuilder buffer = this.stringBuilder.Value;
            buffer.Clear();

            index++; // Skip the quote
            while (index < value.Length)
            {
                int end = value.IndexOf('"', index);
                if (end < 0)
                {
                    throw new InvalidOperationException("Invalid escaped CSV value");
                }

                buffer.Append(value, index, end - index);
                index = end + 2; // Skip the quote and either quote or comma

                if ((end < value.Length - 1) &&
                    (value[end + 1] == '"'))
                {
                    buffer.Append('"');
                }
                else
                {
                    break;
                }
            }

            return buffer.ToString();
        }
    }
}
