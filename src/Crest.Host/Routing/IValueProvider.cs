// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    /// <summary>
    /// Allows a value for a parameter to a routed method to be generated.
    /// </summary>
    internal interface IValueProvider
    {
        /// <summary>
        /// Gets the value to use for the parameter.
        /// </summary>
        object Value { get; }
    }
}