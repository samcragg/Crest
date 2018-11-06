// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess
{
    using System.Linq;
    using Crest.Core.Util;
    using Crest.DataAccess.Expressions;

    /// <summary>
    /// Provides additional methods to <see cref="IQueryable{T}"/>.
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Gets or sets the internal cache for mappings between properties.
        /// </summary>
        internal static MappingCache MappingCache { get; set; }

        /// <summary>
        /// Applies filtering/sorting based on the query string parameters.
        /// </summary>
        /// <typeparam name="T">The type of the data in the data source.</typeparam>
        /// <param name="queryable">The query to apply the transformations to.</param>
        /// <returns>An object that can be used to apply the filtering/sorting.</returns>
        public static QueryOptions<T> Apply<T>(this IQueryable<T> queryable)
        {
            Check.IsNotNull(queryable, nameof(queryable));
            return new QueryOptions<T>(queryable);
        }
    }
}
