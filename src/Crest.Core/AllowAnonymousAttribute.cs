// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Core
{
    using System;

    /// <summary>
    /// Marks a method as allowing access from any user (i.e. skip any
    /// authorization check).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class AllowAnonymousAttribute : Attribute
    {
    }
}
