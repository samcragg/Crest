// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    /// <summary>
    /// Defines a mechanism for retrieving a link to method in a service.
    /// </summary>
    public interface ILinkProvider
    {
        /// <summary>
        /// Provides an object that can be used to create a link to the
        /// specified service.
        /// </summary>
        /// <typeparam name="T">The type of the service.</typeparam>
        /// <param name="relationType">
        /// Determines how the link is related to the current context.
        /// </param>
        /// <returns>An object that can be used to create a link.</returns>
        ILinkService<T> LinkTo<T>(string relationType)
            where T : class;
    }
}
