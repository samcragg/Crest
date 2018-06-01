// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.IO
{
    /// <summary>
    /// Allows the iteration of characters in a string.
    /// </summary>
    internal sealed class StringIterator : ICharIterator
    {
        private readonly string source;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringIterator"/> class.
        /// </summary>
        /// <param name="source">The string to iterate over.</param>
        public StringIterator(string source)
        {
            this.source = source ?? string.Empty;
        }

        /// <inheritdoc />
        public char Current { get; private set; }

        /// <inheritdoc />
        public int Position { get; private set; }

        /// <inheritdoc />
        public bool MoveNext()
        {
            int index = this.Position++;
            if (index < this.source.Length)
            {
                this.Current = this.source[index];
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
