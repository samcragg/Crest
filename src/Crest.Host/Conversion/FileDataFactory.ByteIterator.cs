// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using Crest.Host.IO;

    /// <content>
    /// Contains the nested <see cref="ByteIterator"/> class.
    /// </content>
    internal partial class FileDataFactory
    {
        private sealed class ByteIterator : ICharIterator
        {
            private readonly int length;
            private readonly byte[] source;

            internal ByteIterator(byte[] source, int count)
            {
                this.source = source;
                this.length = count;
            }

            /// <inheritdoc />
            public char Current { get; private set; }

            /// <inheritdoc />
            public int Position { get; private set; }

            /// <inheritdoc />
            public bool MoveNext()
            {
                int index = this.Position++;
                if (index < this.length)
                {
                    this.Current = (char)this.source[index];
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
