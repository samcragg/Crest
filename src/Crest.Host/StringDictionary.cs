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
    /// An optimized dictionary for storing a small amount of values against a
    /// string (ignoring the case).
    /// </summary>
    /// <typeparam name="T">The type of value to store.</typeparam>
    /// <remarks>
    /// <para>
    /// The dictionary supports fetching, adding and clearing of values - it
    /// does not support individual key removal/updating.
    /// </para><para>
    /// The lookup is done as a linear search, therefore, this class is
    /// designed for a small number of values. Also, there is no check for
    /// duplicate keys.
    /// </para>
    /// </remarks>
    internal sealed class StringDictionary<T> : IDictionary<string, T>, IReadOnlyDictionary<string, T>
    {
        private const int InitialSize = 4;
        private string[] keys = new string[InitialSize];
        private T[] values = new T[InitialSize];

        /// <inheritdoc />
        public int Count { get; private set; }

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public ICollection<string> Keys => new ArraySegment<string>(this.keys, 0, this.Count);

        /// <inheritdoc />
        public ICollection<T> Values => new ArraySegment<T>(this.values, 0, this.Count);

        /// <inheritdoc />
        IEnumerable<string> IReadOnlyDictionary<string, T>.Keys => this.Keys;

        /// <inheritdoc />
        IEnumerable<T> IReadOnlyDictionary<string, T>.Values => this.Values;

        /// <inheritdoc />
        public T this[string key]
        {
            get
            {
                int index = this.IndexOf(key);
                return index >= 0 ? this.values[index] : default;
            }

            set => throw new NotSupportedException();
        }

        /// <inheritdoc />
        public void Add(string key, T value)
        {
            this.EnsureSpaceToAdd();

            int index = this.Count++;
            this.keys[index] = key;
            this.values[index] = value;
        }

        /// <inheritdoc />
        public void Clear()
        {
            Array.Clear(this.keys, 0, this.Count);
            Array.Clear(this.values, 0, this.Count);
            this.Count = 0;
        }

        /// <inheritdoc />
        public bool ContainsKey(string key)
        {
            return this.IndexOf(key) >= 0;
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            for (int i = 0; i < this.Count; i++)
            {
                yield return new KeyValuePair<string, T>(this.keys[i], this.values[i]);
            }
        }

        /// <inheritdoc />
        public bool TryGetValue(string key, out T value)
        {
            int index = this.IndexOf(key);
            if (index < 0)
            {
                value = default;
                return false;
            }
            else
            {
                value = this.values[index];
                return true;
            }
        }

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, T>>.Add(KeyValuePair<string, T> item)
        {
            this.Add(item.Key, item.Value);
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<string, T>>.Contains(KeyValuePair<string, T> item)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        void ICollection<KeyValuePair<string, T>>.CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <inheritdoc />
        bool IDictionary<string, T>.Remove(string key)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        bool ICollection<KeyValuePair<string, T>>.Remove(KeyValuePair<string, T> item)
        {
            throw new NotSupportedException();
        }

        private static bool EqualsOrdinalIgnoreCase(string strA, string strB)
        {
            int count = strA.Length;
            if (count != strB.Length)
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                // uppercase both chars - notice that we need just one compare per char
                int charA = strA[i];
                if ((uint)(charA - 'a') <= (uint)('z' - 'a'))
                {
                    charA -= 0x20;
                }

                int charB = strB[i];
                if ((uint)(charB - 'a') <= (uint)('z' - 'a'))
                {
                    charB -= 0x20;
                }

                // Loops with return statements are slower (https://github.com/dotnet/coreclr/issues/9692)
                if (charA != charB)
                {
                    count = -1;
                    break;
                }
            }

            return count >= 0;
        }

        private void EnsureSpaceToAdd()
        {
            if (this.Count == this.keys.Length)
            {
                Array.Resize(ref this.keys, this.keys.Length * 2);
                Array.Resize(ref this.values, this.values.Length * 2);
            }
        }

        private int IndexOf(string key)
        {
            if (key != null)
            {
                for (int i = 0; i < this.Count; i++)
                {
                    if (EqualsOrdinalIgnoreCase(this.keys[i], key))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
    }
}
