// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a method that invoked a route handler.
    /// </summary>
    /// <param name="parameters">The parameters extracted from the request.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The value of the
    /// <c>TResult</c> parameter contains the object to reply with.
    /// </returns>
    public delegate Task<object> RouteMethod(IReadOnlyDictionary<string, object> parameters);
}
