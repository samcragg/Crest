// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Parsing
{
    using System.Collections.Generic;
    using System.Linq;

    /// <content>
    /// Contains the nested helper <see cref="MutableNode"/> class.
    /// </content>
    internal partial class RouteTrieBuilder<T>
    {
        private sealed class MutableNode
        {
            private readonly HashSet<T> values = new HashSet<T>();

            public MutableNode(char key)
            {
                this.Key = key;
            }

            public MutableNode(IMatchNode matcher)
            {
                this.Matcher = matcher;
            }

            public List<MutableNode> Children { get; } = new List<MutableNode>();

            public bool IsEdgeNode => (this.Matcher != null) || (this.values.Count > 0);

            public char Key { get; }

            public IMatchNode Matcher { get; }

            internal MutableNode Add(string key, int index)
            {
                if (index == key.Length)
                {
                    return this;
                }
                else
                {
                    char ch = key[index];
                    MutableNode child = this.FindChild(ch);
                    if (child == null)
                    {
                        child = new MutableNode(ch);
                        this.Children.Add(child);
                    }

                    return child.Add(key, index + 1);
                }
            }

            internal bool AddValue(T value)
            {
                return this.values.Add(value);
            }

            internal T[] GetValues()
            {
                return this.values.ToArray();
            }

            private MutableNode FindChild(char ch)
            {
                foreach (MutableNode child in this.Children)
                {
                    if (child.Key == ch)
                    {
                        return child;
                    }
                }

                return null;
            }
        }
    }
}
