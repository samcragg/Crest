// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Represents the mapping information between two types.
    /// </summary>
    public interface IMappingProvider
    {
        /// <summary>
        /// Gets the type of the destination of the mapped values.
        /// </summary>
        Type Destination { get; }

        /// <summary>
        /// Gets the type of the source of mapped values.
        /// </summary>
        Type Source { get; }

        /// <summary>
        /// Generates an expression that maps the members from the type
        /// represented by <see cref="Source"/> to the <see cref="Destination"/> type.
        /// </summary>
        /// <returns>An expression assigning the properties.</returns>
        Expression GenerateMappings();
    }
}
