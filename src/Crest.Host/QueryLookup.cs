// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Parses the key value pairs out of the query information.
    /// </summary>
    public sealed partial class QueryLookup : ILookup<string, string>
    {
        private readonly Dictionary<string, Grouping> groups =
            new Dictionary<string, Grouping>(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryLookup"/> class.
        /// </summary>
        /// <param name="query">The query information.</param>
        public QueryLookup(string query)
        {
            if (!string.IsNullOrEmpty(query))
            {
                int index = query[0] == '?' ? 1 : 0;
                while (index < query.Length)
                {
                    int start = index;
                    int separator = FindKeyValuePair(query, ref index);
                    if (separator < 0)
                    {
                        this.AddGroup(
                            new StringSegment(query, start, index),
                            default);
                    }
                    else
                    {
                        this.AddGroup(
                            new StringSegment(query, start, separator),
                            new StringSegment(query, separator + 1, index));
                    }

                    index++;
                }
            }
        }

        /// <summary>
        /// Gets the number of keys in the query information.
        /// </summary>
        public int Count => this.groups.Count;

        /// <summary>
        /// Gets the values associated with the specified key in the query
        /// information.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <returns>
        /// The values associated with the specified key, or an empty collection
        /// if the key does not exist.
        /// </returns>
        public IEnumerable<string> this[string key]
        {
            get
            {
                if (this.groups.TryGetValue(key, out Grouping group))
                {
                    return group;
                }
                else
                {
                    return Enumerable.Empty<string>();
                }
            }
        }

        /// <summary>
        /// Determines whether a specified key exists in the query information.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <returns>
        /// <c>true</c> if the specified key exists; otherwise, <c>false</c>.
        /// </returns>
        public bool Contains(string key)
        {
            return this.groups.ContainsKey(key);
        }

        /// <inheritdoc />
        public IEnumerator<IGrouping<string, string>> GetEnumerator()
        {
            return this.groups.Values.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private static byte DecodePercentageValue(StringSegment segment, int index)
        {
            if ((index + 2) >= segment.Count)
            {
                throw new UriFormatException("Invalid percentage escaped data.");
            }

            int high = GetHexValue(segment[index + 1]);
            int low = GetHexValue(segment[index + 2]);
            return (byte)((high * 16) + low);
        }

        private static int FindKeyValuePair(string query, ref int index)
        {
            int separator = -1;
            while (index < query.Length)
            {
                char c = query[index];
                if (c == '=')
                {
                    if (separator == -1)
                    {
                        separator = index;
                    }
                }
                else if (c == '&')
                {
                    break;
                }

                index++;
            }

            return separator;
        }

        private static int GetHexValue(char c)
        {
            uint value = (uint)(c - '0');
            if (value < 10)
            {
                return (int)value;
            }

            // Check uppercase first, as the standard states prefer encoding
            // them in uppercase.
            value = (uint)(c - 'A');
            if (value > 5)
            {
                value = (uint)(c - 'a');
                if (value > 5)
                {
                    throw new UriFormatException("Invalid percentage escaped data.");
                }
            }

            return (int)value + 10;
        }

        private static string UnescapeSegment(StringSegment segment)
        {
            int count = segment.Count;
            byte[] buffer = new byte[count];
            int index = 0;
            for (int i = 0; i < count; i++)
            {
                char c = segment[i];
                if (c == '%')
                {
                    buffer[index] = DecodePercentageValue(segment, i);
                    i += 2;
                }
                else if (c == '+')
                {
                    buffer[index] = (byte)' ';
                }
                else
                {
                    buffer[index] = (byte)c;
                }

                index++;
            }

            return Encoding.UTF8.GetString(buffer, 0, index);
        }

        private void AddGroup(StringSegment key, StringSegment value)
        {
            string keyString = UnescapeSegment(key);

            if (!this.groups.TryGetValue(keyString, out Grouping group))
            {
                group = new Grouping(keyString);
                this.groups.Add(keyString, group);
            }

            group.Add(value);
        }
    }
}
