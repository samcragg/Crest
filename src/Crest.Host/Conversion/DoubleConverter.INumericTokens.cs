// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    /// <content>
    /// Contains the nested <see cref="INumericTokens"/> interface.
    /// </content>
    internal static partial class DoubleConverter
    {
        /// <summary>
        /// Allows for customization when parsing numbers with decimals.
        /// </summary>
        internal interface INumericTokens
        {
            /// <summary>
            /// Determines whether the specified character is a decimal
            /// separator or not.
            /// </summary>
            /// <param name="c">The character to test.</param>
            /// <returns>
            /// <c>true</c> if the character is a decimal separators;
            /// otherwise, <c>false</c>.
            /// </returns>
            bool IsDecimalSeparator(char c);
        }
    }
}
