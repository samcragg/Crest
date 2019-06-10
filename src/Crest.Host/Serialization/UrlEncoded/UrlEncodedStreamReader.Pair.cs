// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

// "Equals" and the comparison operators should be overridden when implementing "IComparable"
// Since this is an internal type, the methods are not being added as they are
// not used and we are in control of the class usage
#pragma warning disable S1210

namespace Crest.Host.Serialization.UrlEncoded
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Crest.Host.Conversion;

    /// <content>
    /// Contains the nested <see cref="Pair"/> struct.
    /// </content>
    internal sealed partial class UrlEncodedStreamReader
    {
        /// <summary>
        /// Represents the parsed parts of a property key with it's associated
        /// value.
        /// </summary>
        /// <remarks>
        /// This is exposed as internal to allow unit testing, as it has some
        /// logic that can be tested in isolation from the enclosing class.
        /// </remarks>
        [DebuggerDisplay("{Key}={Value}")]
        internal readonly struct Pair : IComparable<Pair>
        {
            // This stores the index of the starting character of the part and
            // includes the first character and an imaginary stop at the end (if
            // it doesn't exist), i.e. for this string:
            //
            //     "abc.def"
            //      0123456
            //
            // we'd store the indexes as: 0,4,8
            private readonly int[] indexes;
            private readonly int[] partsAsInteger;

            /// <summary>
            /// Initializes a new instance of the <see cref="Pair"/> struct.
            /// </summary>
            /// <param name="key">The key to parse.</param>
            /// <param name="value">The value for the pair.</param>
            public Pair(string key, string value)
            {
                this.Key = key;
                this.Value = value;
                this.indexes = FindParts(key);
                this.partsAsInteger = ParseParts(key, this.indexes);
            }

            /// <summary>
            /// Gets the key of the pair.
            /// </summary>
            public string Key { get; }

            /// <summary>
            /// Gets the value of the pair.
            /// </summary>
            public string Value { get; }

            private int PartsCount => this.partsAsInteger.Length;

            /// <inheritdoc />
            public int CompareTo(Pair other)
            {
                for (int i = 0; i < this.PartsCount; i++)
                {
                    int result = this.ComparePart(other, i);
                    if (result != 0)
                    {
                        return result;
                    }
                }

                return this.PartsCount - other.PartsCount;
            }

            /// <summary>
            /// Gets the specified part as an integer, if it exists.
            /// </summary>
            /// <param name="part">The index of the part.</param>
            /// <returns>
            /// The integer represented by the specified part index, or a
            /// negative number if the part cannot be converted to an integer
            /// or if it does not exist.
            /// </returns>
            internal int GetArrrayIndex(int part)
            {
                if (part >= this.PartsCount)
                {
                    return -1;
                }
                else
                {
                    return this.partsAsInteger[part];
                }
            }

            /// <summary>
            /// Gets the part of the key at the specified index.
            /// </summary>
            /// <param name="part">The index of the part.</param>
            /// <returns>
            /// The sub-string represented by the specified part index, or
            /// <c>null</c> if it does not exist.
            /// </returns>
            internal string GetPart(int part)
            {
                if (part >= this.PartsCount)
                {
                    return null;
                }
                else
                {
                    GetPart(this.indexes, part, out int start, out int length);
                    return this.Key.Substring(start, length);
                }
            }

            /// <summary>
            /// Determines if the parts of the key in this instance and the
            /// specified object are the same.
            /// </summary>
            /// <param name="other">The instance to compare to.</param>
            /// <param name="part">The index of the part.</param>
            /// <returns>
            /// <c>true</c> if both subparts are the same; otherwise, <c>false</c>.
            /// </returns>
            internal bool PartIsEqual(in Pair other, int part)
            {
                if (part >= this.PartsCount)
                {
                    return false;
                }
                else
                {
                    return this.ComparePart(other, part) == 0;
                }
            }

            private static int[] FindParts(string value)
            {
                // Worst case scenario is each property is one character, hence
                // length / 2 indexes plus the ones at the start/end e.g.
                // "1.2.3.4" has a length of 7
                // (7/2) + 2 == 5
                var separators = new List<int>((value.Length / 2) + 2);
                int index = value.IndexOf('.');
                int start = 0;
                while (index >= 0)
                {
                    if (index != start)
                    {
                        separators.Add(start);
                    }

                    start = index + 1;
                    index = value.IndexOf('.', start);
                }

                separators.Add(start);

                // Avoid adding an empty element at the end if the string
                // already ends with a dot
                if (start != value.Length)
                {
                    // The index always points to the start of the next segment,
                    // which is after the dot, so add an index pointing to one
                    // past the end of the string for that last segment
                    separators.Add(value.Length + 1);
                }

                return separators.ToArray();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void GetPart(int[] indexes, int index, out int start, out int length)
            {
                start = indexes[index];
                length = (indexes[index + 1] - 1) - start;
            }

            private static int[] ParseParts(string value, int[] indexes)
            {
                // indexes contains an extra index for the end, hence length - 1
                int[] integers = new int[indexes.Length - 1];
                ReadOnlySpan<char> valueSpan = value.AsSpan();

                for (int i = 0; i < integers.Length; i++)
                {
                    GetPart(indexes, i, out int start, out int length);
                    ParseResult<ulong> result = IntegerConverter.TryReadUnsignedInt(
                        valueSpan.Slice(start, length),
                        int.MaxValue);

                    // Was it a complete match?
                    if (result.IsSuccess && (result.Length == length))
                    {
                        integers[i] = (int)result.Value;
                    }
                    else
                    {
                        integers[i] = -1;
                    }
                }

                return integers;
            }

            private int CompareCharacters(Pair other, int part)
            {
                GetPart(this.indexes, part, out _, out int thisLength);
                GetPart(other.indexes, part, out _, out int otherLength);
                int result = string.Compare(
                    this.Key,
                    this.indexes[part],
                    other.Key,
                    other.indexes[part],
                    Math.Min(thisLength, otherLength),
                    StringComparison.OrdinalIgnoreCase);

                if (result == 0)
                {
                    return thisLength - otherLength;
                }
                else
                {
                    return result;
                }
            }

            private int ComparePart(in Pair other, int part)
            {
                if (part >= other.PartsCount)
                {
                    // It's comes before us
                    return 1;
                }

                // First compare by integers
                int thisPart = this.partsAsInteger[part];
                int otherPart = other.partsAsInteger[part];
                if ((thisPart >= 0) && (otherPart >= 0))
                {
                    return thisPart - otherPart;
                }

                // Otherwise, lexicographical
                return this.CompareCharacters(other, part);
            }
        }
    }
}
