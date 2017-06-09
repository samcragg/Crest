// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;

    /// <summary>
    /// Allows the custom creation of specific types.
    /// </summary>
    public interface ITypeFactory
    {
        /// <summary>
        /// Determines whether this factory can create the specified type.
        /// </summary>
        /// <param name="type">The type to create.</param>
        /// <returns>
        /// <c>true</c> if <see cref="Create(Type, IServiceProvider)"/> can be
        /// called to create an instance of the specified type; otherwise,
        /// <c>false</c>.
        /// </returns>
        bool CanCreate(Type type);

        /// <summary>
        /// Creates an instance of the specified type.
        /// </summary>
        /// <param name="type">The type to create.</param>
        /// <param name="serviceProvider">Provides services at runtime.</param>
        /// <returns>An instance of the specified type.</returns>
        object Create(Type type, IServiceProvider serviceProvider);
    }
}
