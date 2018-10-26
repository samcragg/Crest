// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess
{
    using System;
    using System.Linq.Expressions;

    /// <content>
    /// Contains the nested <see cref="PropertyMapping"/> struct.
    /// </content>
    public abstract partial class MappingProvider<TSource, TDest>
    {
        /// <summary>
        /// Provides methods to help build a property mapping.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1815:Override equals and operator equals on value types",
            Justification = "This is a short lived object that will not be used in comparisons")]
        protected struct PropertyMapping
        {
            private readonly MappingProvider<TSource, TDest> parent;
            private readonly Expression source;

            /// <summary>
            /// Initializes a new instance of the <see cref="PropertyMapping"/> struct.
            /// </summary>
            /// <param name="parent">The owner of the mapping.</param>
            /// <param name="source">The source property.</param>
            internal PropertyMapping(MappingProvider<TSource, TDest> parent, Expression source)
            {
                this.parent = parent;
                this.source = source;
            }

            /// <summary>
            /// Maps to the specified destination property.
            /// </summary>
            /// <typeparam name="T">The return type of the property.</typeparam>
            /// <param name="property">The expression to access the property.</param>
            public void To<T>(Expression<Func<TDest, T>> property)
            {
                this.parent.mappings.Add(
                    Expression.Assign(property.Body, this.source));
            }
        }
    }
}
