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
    /// Provides construction methods for creating a <see cref="MappingInfo"/>.
    /// </summary>
    /// <typeparam name="TSource">The type of the source of mapped values.</typeparam>
    /// <typeparam name="TDest">The type of the destination of mapped values.</typeparam>
    public partial class MappingInfoBuilder<TSource, TDest>
    {
        private readonly List<Expression> mappings = new List<Expression>();

        /// <summary>
        /// Allows the mapping of the source property to a destination property.
        /// </summary>
        /// <typeparam name="T">The return type of the property.</typeparam>
        /// <param name="property">The expression to access the property.</param>
        /// <returns>A helper object to build the rest of the mapping.</returns>
        public PropertyMapping Map<T>(Expression<Func<TSource, T>> property)
        {
            return new PropertyMapping(this, property.Body);
        }

        /// <summary>
        /// Creates the mapping information.
        /// </summary>
        /// <returns>The mapping information.</returns>
        public MappingInfo ToMappingInfo()
        {
            BlockExpression block = Expression.Block(this.mappings);
            return new MappingInfo(typeof(TDest), typeof(TSource), block);
        }
    }
}
