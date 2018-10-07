// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System;
    using System.Buffers;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Parses the key value pairs out of the query information.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Naming",
        "CA1710:Identifiers should have correct suffix",
        Justification = "Class implements the ILookup interface so has the suffix of Lookup to match.")]
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
                            query.AsSpan(start, index - start),
                            ReadOnlySpan<char>.Empty);
                    }
                    else
                    {
                        this.AddGroup(
                            query.AsSpan(start, separator - start),
                            query.AsSpan(separator + 1, index - separator - 1));
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
        /// Gets or sets the resource pool for obtaining byte arrays.
        /// </summary>
        /// <remarks>
        /// This is exposed for unit testing and defaults to
        /// <see cref="ArrayPool{T}.Shared"/>.
        /// </remarks>
        internal static ArrayPool<byte> BytePool { get; set; } = ArrayPool<byte>.Shared;

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

        private static byte DecodePercentageValue(in ReadOnlySpan<char> segment, int index)
        {
            if ((index + 2) >= segment.Length)
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

        private static string UnescapeSegment(in ReadOnlySpan<char> segment)
        {
            byte[] buffer = BytePool.Rent(segment.Length);
            try
            {
                int length = UnescapeSegment(segment, buffer);
                return Encoding.UTF8.GetString(buffer, 0, length);
            }
            finally
            {
                BytePool.Return(buffer);
            }
        }

        private static int UnescapeSegment(in ReadOnlySpan<char> segment, byte[] buffer)
        {
            int index = 0;
            for (int i = 0; i < segment.Length; i++)
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

            return index;
        }

        private void AddGroup(in ReadOnlySpan<char> key, in ReadOnlySpan<char> value)
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
