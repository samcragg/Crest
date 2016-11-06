// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <content>
    /// Contains the nested helper <see cref="Grouping"/> class.
    /// </content>
    public sealed partial class QueryLookup
    {
        private sealed class Grouping : IGrouping<string, string>, IReadOnlyCollection<string>
        {
            private DataNode last; // We store these in reverse order (makes inserting easier)
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

            public IEnumerator<string> GetEnumerator()
            {
                this.EnsureValuesAreCreated();
                return ((IEnumerable<string>)this.values).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
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
