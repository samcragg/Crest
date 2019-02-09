// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System.Linq.Expressions;

    /// <content>
    /// Contains the nested <see cref="Mapping"/> struct.
    /// </content>
    internal partial class JumpTableGenerator
    {
        private readonly struct Mapping
        {
            public Mapping(string key, Expression body)
            {
                this.Body = body;
                this.HashCode = CaseInsensitiveStringHelper.GetHashCode(key);
                this.Key = key;
            }

            public Expression Body { get; }

            public int HashCode { get; }

            public string Key { get; }
        }
    }
}
