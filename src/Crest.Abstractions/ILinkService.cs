// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System;
    using Crest.Core;

    /// <summary>
    /// Defines a mechanism for creating a link to method in the service.
    /// </summary>
    /// <typeparam name="T">The type of the service.</typeparam>
    public interface ILinkService<out T>
        where T : class
    {
        /// <summary>
        /// Creates a link that can be called by the client to execute the
        /// specified method.
        /// </summary>
        /// <param name="action">
        /// Provides a proxy service that is used to determine which method to
        /// invoke and with what parameters.
        /// </param>
        /// <returns>
        /// A builder that can be used to construct a link to the method called
        /// on the service.
        /// </returns>
        LinkBuilder Calling(Action<T> action);
    }
}
