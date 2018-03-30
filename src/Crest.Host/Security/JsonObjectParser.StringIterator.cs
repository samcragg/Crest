// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Security
{
    using Crest.Host.Serialization;

    /// <content>
    /// Contains the nested helper <see cref="StringIterator"/> class.
    /// </content>
    internal sealed partial class JsonObjectParser
    {
        private class StringIterator : ICharIterator
        {
            private readonly string source;
            private int index = -1;

            public StringIterator(string source)
            {
                this.source = source ?? string.Empty;
                this.MoveNext();
            }

            public char Current { get; private set; }

            public int Position => this.index;

            public bool MoveNext()
            {
                this.index++;
                if (this.index < this.source.Length)
                {
                    this.Current = this.source[this.index];
                    return true;
                }
                else
                {
                    this.Current = default;
                    return false;
                }
            }
        }
    }
}
