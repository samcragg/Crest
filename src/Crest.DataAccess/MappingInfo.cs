// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess
{
    using System;
    using System.Linq.Expressions;
    using Crest.Core.Util;

    /// <summary>
    /// Provides information about to map between two types.
    /// </summary>
    public sealed class MappingInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MappingInfo"/> class.
        /// </summary>
        /// <param name="destination">The destination type.</param>
        /// <param name="source">The type of the source of data.</param>
        /// <param name="mapping">The expression with the mappings.</param>
        public MappingInfo(Type destination, Type source, Expression mapping)
        {
            Check.IsNotNull(destination, nameof(destination));
            Check.IsNotNull(mapping, nameof(mapping));
            Check.IsNotNull(source, nameof(source));

            this.Destination = destination;
            this.Source = source;
            this.Mapping = mapping;
        }

        /// <summary>
        /// Gets the type of the destination of the mapped values.
        /// </summary>
        public Type Destination { get; }

        /// <summary>
        /// Gets an expression that maps the members from the type represented
        /// by <see cref="Source"/> to the <see cref="Destination"/> type.
        /// </summary>
        public Expression Mapping { get; }

        /// <summary>
        /// Gets the type of the source of mapped values.
        /// </summary>
        public Type Source { get; }
    }
}
