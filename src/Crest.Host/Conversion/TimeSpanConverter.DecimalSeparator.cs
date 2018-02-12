// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    /// <content>
    /// Contains the nested <see cref="DecimalSeparator"/> struct.
    /// </content>
    internal static partial class TimeSpanConverter
    {
        private struct DecimalSeparator : DoubleConverter.INumericTokens
        {
            public bool IsDecimalSeparator(char c)
            {
                // Looks crazy to do an if check and return the result of the
                // comparison, however, the generated assembly is better when
                // the method gets inlined than simply "return (...)"
                if ((c == '.') || (c == ','))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
