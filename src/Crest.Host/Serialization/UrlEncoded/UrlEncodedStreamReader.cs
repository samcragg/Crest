// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.UrlEncoded
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Crest.Host.IO;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Allows the reading of values from a stream representing URL encoded data.
    /// </summary>
    internal sealed partial class UrlEncodedStreamReader : ValueReader
    {
        // This class simulates reading over the pairs like a tree. Since the
        // pairs are sorted, we can compare the parts before the current line
        // with the parts of the next line:
        //
        //     Property.Nested1
        //     Property.Nested2
        //
        //         Property
        //         ____|____
        //        |         |
        //     Nested1   Nested2
        private readonly IReadOnlyList<Pair> pairs;
        private int currentIndex;
        private int depth;

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlEncodedStreamReader"/> class.
        /// </summary>
        /// <param name="stream">The stream to read the values from.</param>
        public UrlEncodedStreamReader(Stream stream)
        {
            using (var iterator = new StreamIterator(stream))
            using (var buffer = new StringBuffer())
            {
                var parser = new FormParser(iterator, buffer);
                this.pairs = parser.Pairs;
            }
        }

        /// <summary>
        /// Gets the array index of the current part.
        /// </summary>
        internal int CurrentArrayIndex => this.pairs[this.currentIndex].GetArrrayIndex(this.depth);

        /// <summary>
        /// Gets the current part of the key.
        /// </summary>
        internal string CurrentPart => this.pairs[this.currentIndex].GetPart(this.depth);

        /// <inheritdoc />
        public override bool ReadBoolean()
        {
            string currentValue = this.ReadCurrentValue();
            if (string.Equals("true", currentValue, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else if (string.Equals("false", currentValue, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            else
            {
                throw new FormatException(
                    $"Expected a boolean value at {this.GetCurrentPosition()}");
            }
        }

        /// <inheritdoc />
        public override char ReadChar()
        {
            string currentValue = this.ReadCurrentValue();
            if ((currentValue == null) || (currentValue.Length != 1))
            {
                throw new FormatException(
                    $"Expected a single character at {this.GetCurrentPosition()}");
            }
            else
            {
                return currentValue[0];
            }
        }

        /// <inheritdoc />
        public override bool ReadNull()
        {
            string currentValue = this.ReadCurrentValue();
            return string.Equals("null", currentValue, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public override string ReadString()
        {
            return this.ReadCurrentValue();
        }

        /// <inheritdoc />
        internal override string GetCurrentPosition()
        {
            return "key: '" + this.pairs[this.currentIndex].Key + "'";
        }

        /// <summary>
        /// Moves to the first child of the key part.
        /// </summary>
        internal void MoveToChildren()
        {
            this.depth++;
        }

        /// <summary>
        /// Moves to the next key part at the same level.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the current part has been advanced to another key
        /// part on the same level; otherwise, <c>false</c>.
        /// </returns>
        internal bool MoveToNextSibling()
        {
            if ((this.currentIndex >= (this.pairs.Count - 1)) ||
                !this.HasSamePrefix(this.pairs[this.currentIndex + 1]))
            {
                return false;
            }
            else
            {
                this.currentIndex++;
                return true;
            }
        }

        /// <summary>
        /// Moves up the tree to the parent key part.
        /// </summary>
        internal void MoveToParent()
        {
            this.depth--;
        }

        /// <inheritdoc />
        private protected override ReadOnlySpan<char> ReadTrimmedString()
        {
            return this.pairs[this.currentIndex].Value.Trim().AsSpan();
        }

        private bool HasSamePrefix(in Pair key)
        {
            Pair current = this.pairs[this.currentIndex];
            for (int i = 0; i < this.depth; i++)
            {
                if (!current.PartIsEqual(key, i))
                {
                    return false;
                }
            }

            return true;
        }

        private string ReadCurrentValue()
        {
            return this.pairs[this.currentIndex].Value;
        }
    }
}
