// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Allows the registering of services.
    /// </summary>
    public interface IServiceRegister : IServiceLocator, IDisposable
    {
        /// <summary>
        /// Registers a factory delegate for creating an instance of the
        /// specified type.
        /// </summary>
        /// <param name="serviceType">The service type to register.</param>
        /// <param name="factory">The delegate used to create an instance.</param>
        void RegisterFactory(Type serviceType, Func<object> factory);

        /// <summary>
        /// Registers an action that will be called after service is resolved
        /// just before returning it to caller.
        /// </summary>
        /// <param name="condition">
        /// Determines whether a type should be initialized.
        /// </param>
        /// <param name="initialize">Invoked to initialize an object.</param>
        void RegisterInitializer(Func<Type, bool> condition, Action<object> initialize);

        /// <summary>
        /// Registers multiple known types, determining the services they
        /// implement automatically.
        /// </summary>
        /// <param name="types">The types to add.</param>
        /// <param name="isSingleInstance">
        /// Determines whether a single instance of the type should be created
        /// or if multiple instances can be created.
        /// </param>
        void RegisterMany(IEnumerable<Type> types, Func<Type, bool> isSingleInstance);
    }
}
