// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.IO
{
    using System.Text;
    using Crest.Host.Serialization;

    /// <content>
    /// Contains the nested helper <see cref="Utf8Enumerator"/> class.
    /// </content>
    internal sealed partial class JsonObjectParser
    {
        private class Utf8Enumerator : ICharIterator
        {
            private readonly char[] characters;
            private int index = -1;

            public Utf8Enumerator(byte[] bytes)
            {
                // TODO: We could decode the UTF-8 ourselves to save allocating
                //       another array...
                this.characters = Encoding.UTF8.GetChars(bytes);
                this.MoveNext();
            }

            public char Current { get; private set; }

            public int Position => this.index;

            public bool MoveNext()
            {
                this.index++;
                if (this.index < this.characters.Length)
                {
                    this.Current = this.characters[this.index];
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
