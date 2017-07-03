// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System.Collections.Generic;

    /// <summary>
    /// Compares the version folder names.
    /// </summary>
    internal sealed class StringVersionComparer : IComparer<string>
    {
        /// <inheritdoc />
        public int Compare(string x, string y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return 0;
            }
            else if (x == null)
            {
                return -1;
            }
            else if (y == null)
            {
                return 1;
            }
            else
            {
                return CompareNonNullStrings(x, y);
            }
        }

        private static int CompareNonEmptyEqualLengthStrings(string x, string y)
        {
            // Ignore the case of the first character (it should be a V or v)
            int delta = char.ToUpperInvariant(x[0]) - char.ToUpperInvariant(y[0]);

            int index = 1;
            while (delta == 0)
            {
                if (index == x.Length)
                {
                    break;
                }

                delta = x[index] - y[index];
                index++;
            }

            return delta;
        }

        private static int CompareNonNullStrings(string x, string y)
        {
            if (x.Length < y.Length)
            {
                return -1;
            }
            else if (y.Length < x.Length)
            {
                return 1;
            }
            else
            {
                // You may be worried that we've been given two different empty
                // strings, however, the .NET Core runtime is aggressive with
                // interning them and I couln't get a unit test to create two
                // empty strings with different references, i.e.
                //    object.ReferenceEquals("", new string(' ', 0))
                // returns *true*, even though we are newing the string up!?
                return CompareNonEmptyEqualLengthStrings(x, y);
            }
        }
    }
}
