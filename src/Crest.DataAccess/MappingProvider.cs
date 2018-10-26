// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    /// <summary>
    /// Provides a base class that can be used to create mappings between types.
    /// </summary>
    /// <typeparam name="TSource">The type of the source of mapped values.</typeparam>
    /// <typeparam name="TDest">The type of the destination of mapped values.</typeparam>
    public abstract partial class MappingProvider<TSource, TDest> : IMappingProvider
    {
        private readonly List<Expression> mappings = new List<Expression>();

        /// <inheritdoc />
        public Type Destination => typeof(TDest);

        /// <inheritdoc />
        public Type Source => typeof(TSource);

        /// <inheritdoc />
        public Expression GenerateMappings()
        {
            BlockExpression block = Expression.Block(this.mappings);
            this.mappings.Clear();
            return block;
        }

        /// <summary>
        /// Maps the source property.
        /// </summary>
        /// <typeparam name="T">The return type of the property.</typeparam>
        /// <param name="property">The expression to access the property.</param>
        /// <returns>A helper object to build the rest of the mapping.</returns>
        protected PropertyMapping Map<T>(Expression<Func<TSource, T>> property)
        {
            return new PropertyMapping(this, property.Body);
        }
    }
}
