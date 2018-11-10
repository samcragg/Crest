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
    public sealed class LinkCollection : ICollection<Link>, IReadOnlyCollection<Link>
    {
        private readonly List<KeyValuePair<string, Link>> links =
            new List<KeyValuePair<string, Link>>();

        /// <summary>
        /// Gets the number of links contained in the collection.
        /// </summary>
        public int Count => this.links.Count;

        /// <inheritdoc />
        bool ICollection<Link>.IsReadOnly => false;

        /// <summary>
        /// Adds a link to the collection.
        /// </summary>
        /// <param name="relationType">
        /// Determines how the current context is related to the link.
        /// </param>
        /// <param name="link">Represents the target resource.</param>
        public void Add(string relationType, Link link)
        {
            Check.IsNotNull(relationType, nameof(relationType));
            Check.IsNotNull(link, nameof(link));

            if (this.ContainsKey(relationType))
            {
                throw new ArgumentException(
                    "A link with the same relation already exists.",
                    nameof(relationType));
            }

            this.links.Add(new KeyValuePair<string, Link>(relationType, link));
        }

        /// <summary>
        /// Removes all links from the collection.
        /// </summary>
        public void Clear()
        {
            this.links.Clear();
        }

        /// <summary>
        /// Determines whether the collection contains a specific relation type.
        /// </summary>
        /// <param name="item">The link to locate.</param>
        /// <returns>
        /// <c>true</c> if <c>link</c> is found in the collection; otherwise,
        /// <c>false</c>.
        /// </returns>
        public bool Contains(Link item)
        {
            return this.links.Any(kvp => kvp.Value.Equals(item));
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
            return this.links.Any(kvp => RelationIsEqual(kvp, relationType));
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
            Check.IsNotNull(array, nameof(array));
            Check.IsPositive(arrayIndex, nameof(arrayIndex));
            if ((arrayIndex + this.links.Count) > array.Length)
            {
                throw new ArgumentException("Destination array was not long enough.");
            }

            for (int i = 0; i < this.links.Count; i++)
            {
                array[arrayIndex + i] = this.links[i].Value;
            }
        }

        /// <inheritdoc />
        public IEnumerator<Link> GetEnumerator()
        {
            for (int i = 0; i < this.links.Count; i++)
            {
                yield return this.links[i].Value;
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
            for (int i = 0; i < this.links.Count; i++)
            {
                if (this.links[i].Value.Equals(item))
                {
                    this.links.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the first occurrence of a specific relationship from the
        /// collection.
        /// </summary>
        /// <param name="relationType">
        /// The link relation to remove from the collection.
        /// </param>
        /// <returns>
        /// <c>true</c> if <c>link</c> was successfully removed from the
        /// collection; otherwise, <c>false</c> if it was not found.
        /// </returns>
        public bool Remove(string relationType)
        {
            for (int i = 0; i < this.links.Count; i++)
            {
                if (RelationIsEqual(this.links[i], relationType))
                {
                    this.links.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        void ICollection<Link>.Add(Link item)
        {
            throw new NotSupportedException("Links must be added with their relation type.");
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private static bool RelationIsEqual(in KeyValuePair<string, Link> kvp, string relationType)
        {
            return string.Equals(kvp.Key, relationType, StringComparison.OrdinalIgnoreCase);
        }
    }
}
