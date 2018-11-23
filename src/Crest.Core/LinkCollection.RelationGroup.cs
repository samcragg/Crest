// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Core
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    /// <content>
    /// Contains the nested <see cref="RelationGroup"/> class.
    /// </content>
    public sealed partial class LinkCollection
    {
        private sealed class RelationGroup : IGrouping<string, Link>, IReadOnlyCollection<Link>
        {
            private readonly int first;
            private readonly int last;
            private readonly Link[] links;

            public RelationGroup(Link[] links, string relation, int first, int last)
            {
                this.first = first;
                this.last = last + 1;
                this.links = links;
                this.Key = relation;
            }

            public int Count => this.last - this.first;

            public string Key { get; }

            public IEnumerator<Link> GetEnumerator()
            {
                for (int i = this.first; i < this.last; i++)
                {
                    yield return this.links[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }
    }
}
