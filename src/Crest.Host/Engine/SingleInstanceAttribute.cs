// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Engine
{
    using System;

    /// <summary>
    /// Marks a class as being registered as a single instance (i.e. it will
    /// only be created once).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class SingleInstanceAttribute : Attribute
    {
    }
}
