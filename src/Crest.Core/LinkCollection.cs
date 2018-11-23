// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Core.Util;

    /// <summary>
    /// Represents a collection of links and their relationship with the
    /// current context.
    /// </summary>
    public sealed partial class LinkCollection : ICollection<Link>, ILookup<string, Link>, IReadOnlyCollection<Link>
    {
        private const int DefaultCapacity = 4;
        private Link[] links = Array.Empty<Link>();

        /// <summary>
        /// Gets the number of links contained in the collection.
        /// </summary>
        public int Count { get; private set; }

        /// <inheritdoc />
        bool ICollection<Link>.IsReadOnly => false;

        /// <inheritdoc />
        public IEnumerable<Link> this[string key]
        {
            get
            {
                int index = this.BinarySearch(key);
                if (index < 0)
                {
                    return Enumerable.Empty<Link>();
                }
                else
                {
                    return new RelationGroup(
                        this.links,
                        key,
                        this.FindGroupStart(index, key),
                        this.FindGroupEnd(index, key));
                }
            }
        }

        /// <inheritdoc />
        public void Add(Link item)
        {
            Check.IsNotNull(item, nameof(item));

            // From the documentation for the return of Array.BinarySearch:
            //     The index of the specified value in the specified array, if
            //     value is found; otherwise, a negative number. If value is
            //     not found ... the negative number returned is the bitwise
            //     complement of the index of the first element that is larger
            //     than value.
            int index = this.BinarySearch(item.RelationType);
            if (index >= 0)
            {
                // We've found a link with the same relation, insert after the
                // end (hence +1) so we maintain insertion order within the group
                this.InsertAt(this.FindGroupEnd(index, item.RelationType) + 1, item);
            }
            else
            {
                this.InsertAt(~index, item);
            }
        }

        /// <summary>
        /// Adds a resource reference to the collection.
        /// </summary>
        /// <param name="relationType">
        /// Determines how the current context is related to the link.
        /// </param>
        /// <param name="reference">Represents the target resource.</param>
        public void Add(string relationType, Uri reference)
        {
            this.Add(new Link(relationType, reference));
        }

        /// <summary>
        /// Removes all links from the collection.
        /// </summary>
        public void Clear()
        {
            Array.Clear(this.links, 0, this.Count);
            this.Count = 0;
        }

        /// <summary>
        /// Determines whether the collection contains a specific link.
        /// </summary>
        /// <param name="item">The link to locate.</param>
        /// <returns>
        /// <c>true</c> if <c>link</c> is found in the collection; otherwise,
        /// <c>false</c>.
        /// </returns>
        public bool Contains(Link item)
        {
            return Array.IndexOf(this.links, item) >= 0;
        }

        /// <summary>
        /// Determines whether the collection contains a specific relation type.
        /// </summary>
        /// <param name="relationType">The type of link relation to locate.</param>
        /// <returns>
        /// <c>true</c> if a relation is found in the collection; otherwise,
        /// <c>false</c>.
        /// </returns>
        public bool ContainsKey(string relationType)
        {
            return this.BinarySearch(relationType) >= 0;
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a
        /// particular index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional array that is the destination of the elements
        /// copied from the collection.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in array at which copying begins.
        /// </param>
        public void CopyTo(Link[] array, int arrayIndex)
        {
            Array.Copy(this.links, 0, array, arrayIndex, this.Count);
        }

        /// <inheritdoc />
        public IEnumerator<Link> GetEnumerator()
        {
            for (int i = 0; i < this.Count; i++)
            {
                yield return this.links[i];
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific link from the collection.
        /// </summary>
        /// <param name="item">The link to remove from the collection.</param>
        /// <returns>
        /// <c>true</c> if <c>link</c> was successfully removed from the
        /// collection; otherwise, <c>false</c> if it was not found.
        /// </returns>
        public bool Remove(Link item)
        {
            int index = Array.IndexOf(this.links, item);
            if (index < 0)
            {
                return false;
            }

            this.Count--;
            if (index < this.Count)
            {
                Array.Copy(this.links, index + 1, this.links, index, this.Count - index);
            }

            this.links[this.Count] = null;
            return true;
        }

        /// <inheritdoc />
        bool ILookup<string, Link>.Contains(string key)
        {
            return this.ContainsKey(key);
        }

        /// <inheritdoc />
        IEnumerator<IGrouping<string, Link>> IEnumerable<IGrouping<string, Link>>.GetEnumerator()
        {
            int index = 0;
            while (index < this.Count)
            {
                string relation = this.links[index].RelationType;
                int end = this.FindGroupEnd(index, relation);
                yield return new RelationGroup(this.links, relation, index, end);
                index = end + 1;
            }
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private static void InsertAt(Link[] source, Link[] destination, int index, int count, Link item)
        {
            if (index < count)
            {
                Array.Copy(source, index, destination, index + 1, count - index);
            }

            destination[index] = item;
        }

        private int BinarySearch(string relation)
        {
            int lo = 0;
            int hi = this.Count - 1;
            while (lo <= hi)
            {
                int index = lo + ((hi - lo) / 2);
                int order = string.CompareOrdinal(this.links[index].RelationType, relation);

                if (order == 0)
                {
                    return index;
                }
                else if (order < 0)
                {
                    lo = index + 1;
                }
                else
                {
                    hi = index - 1;
                }
            }

            // This matches the contract of Array.BinarySearch
            return ~lo;
        }

        private int FindGroupEnd(int index, string relation)
        {
            for (index++; index < this.Count; index++)
            {
                if (!this.links[index].RelationType.Equals(relation, StringComparison.Ordinal))
                {
                    break;
                }
            }

            return index - 1;
        }

        private int FindGroupStart(int index, string relation)
        {
            for (index--; index >= 0; index--)
            {
                if (!this.links[index].RelationType.Equals(relation, StringComparison.Ordinal))
                {
                    break;
                }
            }

            return index + 1;
        }

        private void InsertAt(int index, Link item)
        {
            if (this.links.Length == this.Count)
            {
                int newSize = this.Count * 2;
                var newLinks = new Link[newSize == 0 ? DefaultCapacity : newSize];
                Array.Copy(this.links, 0, newLinks, 0, index);
                InsertAt(this.links, newLinks, index, this.Count, item);
                this.links = newLinks;
            }
            else
            {
                InsertAt(this.links, this.links, index, this.Count, item);
            }

            this.Count++;
        }
    }
}
