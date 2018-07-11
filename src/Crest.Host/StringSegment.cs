// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Contains information about a segment of a string.
    /// </summary>
    internal struct StringSegment : IReadOnlyList<char>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringSegment"/> struct.
        /// </summary>
        /// <param name="value">The string value.</param>
        public StringSegment(string value)
        {
            this.String = value ?? string.Empty;
            this.Start = 0;
            this.End = this.String.Length;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringSegment"/> struct.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <param name="start">The start index of the substring.</param>
        /// <param name="end">The end index of the substring.</param>
        public StringSegment(string value, int start, int end)
        {
            this.String = value;
            this.Start = start;
            this.End = end;
        }

        /// <summary>
        /// Gets the number of characters in the substring.
        /// </summary>
        public int Count => this.End - this.Start;

        /// <summary>
        /// Gets the index of the character after the substring.
        /// </summary>
        public int End { get; }

        /// <summary>
        /// Gets the index of the first character in the substring.
        /// </summary>
        public int Start { get; }

        /// <summary>
        /// Gets the original string value.
        /// </summary>
        public string String { get; }

        /// <summary>
        /// Gets the character at the specified index relative to the start of
        /// the substring.
        /// </summary>
        /// <param name="index">The index of the character.</param>
        /// <returns>The character at the specified index.</returns>
        public char this[int index] => this.String[this.Start + index];

        /// <summary>
        /// Determines whether the substring pointed to by this instance matches
        /// the specified string.
        /// </summary>
        /// <param name="value">The value to compare to.</param>
        /// <param name="comparisonType">
        /// Specifies the rules to use in the comparison.
        /// </param>
        /// <returns>
        /// <c>true</c> if the substring is equal to the specified value;
        /// otherwise <c>false</c>.
        /// </returns>
        public bool Equals(string value, StringComparison comparisonType)
        {
            if ((value != null) && (value.Length > this.Count))
            {
                return false;
            }

            int compare = string.Compare(
                this.String,
                this.Start,
                value,
                0,
                this.Count,
                comparisonType);

            return compare == 0;
        }

        /// <summary>
        /// Gets the characters that form the substring.
        /// </summary>
        /// <returns>The characters of the sub-section of the string.</returns>
        public IEnumerator<char> GetEnumerator()
        {
            for (int i = this.Start; i < this.End; i++)
            {
                yield return this.String[i];
            }
        }

        /// <summary>
        /// Gets the substring represented by this instance.
        /// </summary>
        /// <returns>The substring from the original string.</returns>
        public override string ToString()
        {
            return this.String.Substring(this.Start, this.Count);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Gets a span that represents this instance.
        /// </summary>
        /// <returns>A span.</returns>
        internal ReadOnlySpan<char> CreateSpan()
        {
            return new ReadOnlySpan<char>(this.String.ToCharArray(), this.Start, this.Count);
        }
    }
}
