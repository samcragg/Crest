// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Core
{
    using System;

    /// <summary>
    /// Marks a single parameter as receiving the request body.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class FromBodyAttribute : Attribute
    {
    }
}
