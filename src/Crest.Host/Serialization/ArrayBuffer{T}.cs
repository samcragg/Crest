// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;

    /// <summary>
    /// Helper for creating arrays when the size is not know ahead of time.
    /// </summary>
    /// <typeparam name="T">The element type for the array.</typeparam>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1815:Override equals and operator equals on value types",
        Justification = "This is a helper object used by the generated serializers that will not be used in comparisons")]
    internal struct ArrayBuffer<T>
    {
        private int count;
        private T[] items;

        /// <summary>
        /// Adds the specified item to the buffer.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            if (this.items == null)
            {
                this.items = new T[4];
            }
            else if (this.items.Length == this.count)
            {
                this.IncreaseStorage();
            }

            this.items[this.count++] = item;
        }

        /// <summary>
        /// Returns an array containing all the items added to the buffer.
        /// </summary>
        /// <returns>An array of items.</returns>
        public T[] ToArray()
        {
            if (this.items == null)
            {
                return Array.Empty<T>();
            }
            else if (this.items.Length == this.count)
            {
                return this.items;
            }
            else
            {
                var result = new T[this.count];
                Array.Copy(this.items, 0, result, 0, this.count);
                return result;
            }
        }

        private void IncreaseStorage()
        {
            var newItems = new T[this.count * 2];
            Array.Copy(this.items, 0, newItems, 0, this.count);
            this.items = newItems;
        }
    }
}
