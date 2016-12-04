// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Reflection;

    /// <content>
    /// Contains the nested <see cref="Route"/> struct.
    /// </content>
    internal sealed partial class RouteMapper
    {
        private class Route
        {
            private MethodInfo[] methods;
            private long[] versions;

            public void Add(MethodInfo method, int from, int to)
            {
                if (this.methods == null)
                {
                    this.methods = new[] { method };
                    this.versions = new[] { MakeVersion(from, to) };
                }
                else
                {
                    this.CheckForOverlap(method, from, to);

                    int size = this.methods.Length;
                    Array.Resize(ref this.methods, size + 1);
                    this.methods[size] = method;

                    Array.Resize(ref this.versions, size + 1);
                    this.versions[size] = MakeVersion(from, to);
                }
            }

            public MethodInfo Match(int version)
            {
                for (int i = 0; i < this.versions.Length; i++)
                {
                    if (InsideVersionRange(this.versions[i], version))
                    {
                        return this.methods[i];
                    }
                }

                return null;
            }

            private static bool InsideVersionRange(long range, int version)
            {
                int from, to;
                SplitVersion(range, out from, out to);

                // Use bitwise and to avoid a branch...
                return (from <= version) & (version <= to);
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

            private void CheckForOverlap(MethodInfo method, int minimum, int maximum)
            {
                for (int i = 0; i < this.versions.Length; i++)
                {
                    int from, to;
                    SplitVersion(this.versions[i], out from, out to);
                    if ((minimum <= to) && (maximum >= from))
                    {
                        throw new InvalidOperationException(
                            method.Name + " specifies versions that overlap another method.");
                    }
                }
            }
        }
    }
}
