// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System;

    /// <summary>
    /// Allows the registering of services limited to the current request scope.
    /// </summary>
    public interface IScopedServiceRegister
    {
        /// <summary>
        /// Specifies an object to use if the specific service is asked for.
        /// </summary>
        /// <param name="serviceType">The service type to register.</param>
        /// <param name="instance">The object to return for the service.</param>
        /// <remarks>
        /// This will replace any registrations for the specified service.
        /// </remarks>
        void UseInstance(Type serviceType, object instance);
    }
}
