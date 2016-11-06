// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Contains information about the incoming request.
    /// </summary>
    public interface IRequestData
    {
        /// <summary>
        /// Gets the method to be invoked that handles the matched route.
        /// </summary>
        MethodInfo Handler { get; }

        /// <summary>
        /// Gets the requested URL.
        /// </summary>
        Uri Url { get; }
    }
}
