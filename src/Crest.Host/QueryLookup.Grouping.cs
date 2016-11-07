// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <content>
    /// Contains the nested helper <see cref="Grouping"/> class.
    /// </content>
    public sealed partial class QueryLookup
    {
        private sealed class Grouping : IGrouping<string, string>, ICollection<string>, IReadOnlyCollection<string>
        {
            private DataNode last; // A linked list stored in reverse order (makes inserting easier)
            private string[] values;

            internal Grouping(string key)
            {
                this.Key = key;
            }

            public int Count
            {
                get
                {
                    this.EnsureValuesAreCreated();
                    return this.values.Length;
                }
            }

            public string Key
            {
                get;
            }

            bool ICollection<string>.IsReadOnly
            {
                get { return true; }
            }

            public bool Contains(string item)
            {
                this.EnsureValuesAreCreated();
                return this.values.Contains(item, StringComparer.Ordinal);
            }

            public void CopyTo(string[] array, int arrayIndex)
            {
                this.EnsureValuesAreCreated();
                this.values.CopyTo(array, arrayIndex);
            }

            public IEnumerator<string> GetEnumerator()
            {
                this.EnsureValuesAreCreated();
                return ((IEnumerable<string>)this.values).GetEnumerator();
            }

            void ICollection<string>.Add(string item)
            {
                throw new NotSupportedException();
            }

            void ICollection<string>.Clear()
            {
                throw new NotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            bool ICollection<string>.Remove(string item)
            {
                throw new NotSupportedException();
            }

            internal void Add(StringSegment value)
            {
                System.Diagnostics.Debug.Assert(this.values == null, "Cannot add to a group that has been iterated over.");
                var node = new DataNode(this.last, value);
                this.last = node;
            }

            private void EnsureValuesAreCreated()
            {
                if (this.values != null)
                {
                    return;
                }

                int count = 0;
                DataNode node = this.last;
                while (node != null)
                {
                    count++;
                    node = node.Previous;
                }

                this.values = new string[count];

                int index = count - 1;
                node = this.last;
                while (node != null)
                {
                    this.values[index] = UnescapeSegment(node.Value);
                    index--;
                    node = node.Previous;
                }

                this.last = null;
            }

            private sealed class DataNode
            {
                internal DataNode(DataNode previous, StringSegment value)
                {
                    this.Previous = previous;
                    this.Value = value;
                }

                internal DataNode Previous { get; }

                internal StringSegment Value { get; }
            }
        }
    }
}
