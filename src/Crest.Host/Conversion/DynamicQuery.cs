// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;

    /// <summary>
    /// Represents the unparsed parts of the query string.
    /// </summary>
    internal sealed partial class DynamicQuery : DynamicObject, IReadOnlyDictionary<string, string[]>
    {
        private readonly Dictionary<string, string[]> values;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicQuery"/> class.
        /// </summary>
        /// <param name="query">The query parameters.</param>
        /// <param name="parameters">The parsed parameters.</param>
        public DynamicQuery(
            ILookup<string, string> query,
            IReadOnlyDictionary<string, object> parameters)
        {
            this.values = query
                .Where(g => !parameters.ContainsKey(g.Key))
                .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public int Count => this.values.Count;

        /// <inheritdoc />
        public IEnumerable<string> Keys => this.values.Keys;

        /// <inheritdoc />
        public IEnumerable<string[]> Values => this.values.Values;

        /// <inheritdoc />
        public string[] this[string key] => this.values[key];

        /// <inheritdoc />
        public bool ContainsKey(string key)
        {
            return this.values.ContainsKey(key);
        }

        /// <inheritdoc />
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return this.values.Keys;
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
        {
            return this.values.GetEnumerator();
        }

        /// <inheritdoc />
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (this.values.TryGetValue(binder.Name, out string[] member))
            {
                result = new DynamicString(member);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        /// <inheritdoc />
        public bool TryGetValue(string key, out string[] value)
        {
            return this.values.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
