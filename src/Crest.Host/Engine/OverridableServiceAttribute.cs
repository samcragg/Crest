// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;

    /// <summary>
    /// Allows a type implementing a service to be overridden in another
    /// assembly (i.e. prevents it being registered by default).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class OverridableServiceAttribute : Attribute
    {
    }
}
