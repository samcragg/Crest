// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.Reflection.Emit;

    /// <content>
    /// Contains the nested <see cref="Mapping"/> class.
    /// </content>
    internal sealed partial class JumpTableGenerator
    {
        private class Mapping
        {
            public Action<ILGenerator> Body { get; set; }

            public uint HashCode { get; set; }

            public string Key { get; set; }
        }
    }
}
