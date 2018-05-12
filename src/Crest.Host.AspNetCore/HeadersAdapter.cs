// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.AspNetCore
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// Wraps the <see cref="IHeaderDictionary"/> into a <see cref="IReadOnlyDictionary{TKey, TValue}"/>.
    /// </summary>
    internal sealed class HeadersAdapter : IReadOnlyDictionary<string, string>
    {
        private readonly IHeaderDictionary headers;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeadersAdapter"/> class.
        /// </summary>
        /// <param name="headers">The headers to wrap.</param>
        public HeadersAdapter(IHeaderDictionary headers)
        {
            this.headers = headers;
        }

        /// <inheritdoc />
        public int Count => this.headers.Count;

        /// <inheritdoc />
        public IEnumerable<string> Keys => this.headers.Keys;

        /// <inheritdoc />
        public IEnumerable<string> Values => this.headers.Values.Select(x => x.ToString());

        /// <inheritdoc />
        public string this[string key]
        {
            get
            {
                if (!this.headers.TryGetValue(key, out StringValues value))
                {
                    throw new KeyNotFoundException();
                }

                return value.ToString();
            }
        }

        /// <inheritdoc />
        public bool ContainsKey(string key)
        {
            return this.headers.ContainsKey(key);
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach (KeyValuePair<string, StringValues> kvp in this.headers)
            {
                yield return new KeyValuePair<string, string>(kvp.Key, kvp.Value);
            }
        }

        /// <inheritdoc />
        public bool TryGetValue(string key, out string value)
        {
            if (this.headers.TryGetValue(key, out StringValues header))
            {
                value = header.ToString();
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
