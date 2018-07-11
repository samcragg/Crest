// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;

    /// <content>
    /// Contains the nested <see cref="Versions"/> class.
    /// </content>
    internal sealed partial class RouteMapper
    {
        private class Versions
        {
            private Target[] targets;
            private long[] versions;

            internal IReadOnlyList<Target> Targets => this.targets;

            public void Add(Target target, int from, int to)
            {
                if (this.targets == null)
                {
                    this.targets = new[] { target };
                    this.versions = new[] { MakeVersion(from, to) };
                }
                else
                {
                    this.CheckForOverlap(target, from, to);

                    int size = this.targets.Length;
                    Array.Resize(ref this.targets, size + 1);
                    this.targets[size] = target;

                    Array.Resize(ref this.versions, size + 1);
                    this.versions[size] = MakeVersion(from, to);
                }
            }

            public Target Match(int version)
            {
                for (int i = 0; i < this.versions.Length; i++)
                {
                    if (InsideVersionRange(this.versions[i], version))
                    {
                        return this.targets[i];
                    }
                }

                return default;
            }

            private static bool InsideVersionRange(long range, int version)
            {
                SplitVersion(range, out int from, out int to);

                // Use bitwise and to avoid a branch...
#pragma warning disable S2178 // Short-circuit logic should be used in boolean contexts
                return (from <= version) & (version <= to);
#pragma warning restore S2178
            }

            private static long MakeVersion(long from, long to)
            {
                return (to << 32) | from;
            }

            private static void SplitVersion(long range, out int from, out int to)
            {
                // range stores 'to' in the high part and 'from' in the low part
                from = (int)range;
                to = (int)(range >> 32);
            }

            private void CheckForOverlap(Target target, int minimum, int maximum)
            {
                for (int i = 0; i < this.versions.Length; i++)
                {
                    SplitVersion(this.versions[i], out int from, out int to);
                    if ((minimum <= to) && (maximum >= from))
                    {
                        throw new InvalidOperationException(
                            target.Method.Name + " specifies versions that overlap another method.");
                    }
                }
            }
        }
    }
}
