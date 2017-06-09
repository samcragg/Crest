// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides instances of classes that have been marked as configuration.
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Determines whether the specified type can be initialized with this
        /// service or not.
        /// </summary>
        /// <param name="type">The type information.</param>
        /// <returns>
        /// <c>true</c> if new instances of the specified type should be passed
        /// to <see cref="InitializeInstance(object, IServiceProvider)"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        bool CanConfigure(Type type);

        /// <summary>
        /// Initializes the specified object by setting configuration options
        /// on its properties.
        /// </summary>
        /// <param name="instance">The object to initialize.</param>
        /// <param name="serviceProvider">Provides services at runtime.</param>
        void InitializeInstance(object instance, IServiceProvider serviceProvider);

        /// <summary>
        /// Allows the providers to be initialized.
        /// </summary>
        /// <param name="discoveredTypes">
        /// All the types that have been discovered at runtime.
        /// </param>
        /// <returns>The result of the asynchronous operation.</returns>
        Task InitializeProviders(IEnumerable<Type> discoveredTypes);
    }
}
