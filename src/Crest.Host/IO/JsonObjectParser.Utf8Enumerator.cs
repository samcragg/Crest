// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.IO
{
    using System.Text;

    /// <content>
    /// Contains the nested helper <see cref="Utf8Enumerator"/> class.
    /// </content>
    internal sealed partial class JsonObjectParser
    {
        private class Utf8Enumerator : ICharIterator
        {
            private readonly char[] characters;

            public Utf8Enumerator(byte[] bytes)
            {
                this.characters = Encoding.UTF8.GetChars(bytes);
                this.MoveNext();
            }

            public char Current { get; private set; }

            public int Position { get; private set; } = -1;

            public bool MoveNext()
            {
                this.Position++;
                if (this.Position < this.characters.Length)
                {
                    this.Current = this.characters[this.Position];
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
