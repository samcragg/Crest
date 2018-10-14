// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess
{
    using System.Linq;
    using System.Reflection;
    using Crest.DataAccess.Expressions;
    using Crest.DataAccess.Parsing;

    /// <summary>
    /// Allows additional query options to a data source.
    /// </summary>
    /// <typeparam name="T">The type of the data in the data source.</typeparam>
    public readonly struct QueryOptions<T>
    {
        private readonly IQueryable<T> query;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryOptions{T}"/> struct.
        /// </summary>
        /// <param name="query">The query to modify.</param>
        internal QueryOptions(IQueryable<T> query)
        {
            this.query = query;
        }

        /// <summary>
        /// Filters a sequence of values based on the query string.
        /// </summary>
        /// <param name="value">Contains the dynamic query information.</param>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> that contains elements that satisfy
        /// the conditions specified in the query string.
        /// </returns>
        public IQueryable<T> Filter(dynamic value)
        {
            return this.Filter<T>(value);
        }

        /// <summary>
        /// Filters a sequence of values based on the query string.
        /// </summary>
        /// <typeparam name="TSource">
        /// The exposed service type that should be filtered if different to
        /// the type of the data source.
        /// </typeparam>
        /// <param name="value">Contains the dynamic query information.</param>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> that contains elements that satisfy
        /// the conditions specified in the query string.
        /// </returns>
        public IQueryable<T> Filter<TSource>(dynamic value)
        {
            QueryParser parser = Parse(value);
            QueryableExpressionBuilder builder = CreateBuilder();
            return ApplyFilter<TSource>(this.query, parser, builder);
        }

        /// <summary>
        /// Filters and sorts a sequence of values based on the query string.
        /// </summary>
        /// <param name="value">Contains the dynamic query information.</param>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> that contains elements that satisfy
        /// and are sorted according the conditions to specified in the query
        /// string.
        /// </returns>
        public IQueryable<T> FilterAndSort(dynamic value)
        {
            return this.FilterAndSort<T>(value);
        }

        /// <summary>
        /// Filters and sorts a sequence of values based on the query string.
        /// </summary>
        /// <typeparam name="TSource">
        /// The exposed service type that should be filtered if different to
        /// the type of the data source.
        /// </typeparam>
        /// <param name="value">Contains the dynamic query information.</param>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> that contains elements that satisfy
        /// and are sorted according the conditions to specified in the query
        /// string.
        /// </returns>
        public IQueryable<T> FilterAndSort<TSource>(dynamic value)
        {
            QueryParser parser = Parse(value);
            QueryableExpressionBuilder builder = CreateBuilder();

            IQueryable<T> filtered = ApplyFilter<TSource>(this.query, parser, builder);
            return ApplySort<TSource>(filtered, parser, builder);
        }

        /// <summary>
        /// Sorts a sequence of values based on the query string.
        /// </summary>
        /// <param name="value">Contains the dynamic query information.</param>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> that contains elements that are
        /// sorted according to the conditions in the query string.
        /// </returns>
        public IQueryable<T> Sort(dynamic value)
        {
            return this.Sort<T>(value);
        }

        /// <summary>
        /// Sorts a sequence of values based on the query string.
        /// </summary>
        /// <typeparam name="TSource">
        /// The exposed service type that should be filtered if different to
        /// the type of the data source.
        /// </typeparam>
        /// <param name="value">Contains the dynamic query information.</param>
        /// <returns>
        /// An <see cref="IQueryable{T}"/> that contains elements that are
        /// sorted according to the conditions in the query string.
        /// </returns>
        public IQueryable<T> Sort<TSource>(dynamic value)
        {
            QueryParser parser = Parse(value);
            QueryableExpressionBuilder builder = CreateBuilder();
            return ApplySort<TSource>(this.query, parser, builder);
        }

        private static IQueryable<T> ApplyFilter<TSource>(
            IQueryable queryable,
            QueryParser parser,
            QueryableExpressionBuilder builder)
        {
            foreach (FilterInfo filter in parser.GetFilters(typeof(TSource)))
            {
                queryable = builder.CreateFilter(queryable, filter.Property, filter.Method, filter.Value);
            }

            return (IQueryable<T>)queryable;
        }

        private static IQueryable<T> ApplySort<TSource>(
            IQueryable queryable,
            QueryParser parser,
            QueryableExpressionBuilder builder)
        {
            foreach ((PropertyInfo property, SortDirection direction) in parser.GetSorting(typeof(TSource)))
            {
                queryable = builder.CreateSort(queryable, property, direction);
            }

            return (IQueryable<T>)queryable;
        }

        private static QueryableExpressionBuilder CreateBuilder()
        {
            return new QueryableExpressionBuilder(QueryableExtensions.MappingCache);
        }

        private static QueryParser Parse(object value)
        {
            return new QueryParser(new DataSource(value));
        }
    }
}
