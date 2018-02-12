// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    /// <content>
    /// Contains the nested <see cref="Triplet"/> struct.
    /// </content>
    internal static partial class TimeSpanConverter
    {
        private struct Triplet
        {
            public double First;

            public double Second;

            public double Third;
        }
    }
}
