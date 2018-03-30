// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    /// <summary>
    /// Allows the iteration of a series of characters.
    /// </summary>
    internal interface ICharIterator
    {
        /// <summary>
        /// Gets the current character.
        /// </summary>
        char Current { get; }

        /// <summary>
        /// Gets the character position relative to the start.
        /// </summary>
        int Position { get; }

        /// <summary>
        /// Attempts to read the next character from the stream.
        /// </summary>
        /// <returns>
        /// <c>true</c> if another character was read from the stream;
        /// otherwise, <c>false</c>.
        /// </returns>
        bool MoveNext();
    }
}