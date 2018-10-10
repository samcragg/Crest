// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;

    /// <summary>
    /// Represents the unparsed parts of the query string.
    /// </summary>
    internal sealed partial class DynamicQuery : DynamicObject
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
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return this.values.Keys;
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
    }
}
