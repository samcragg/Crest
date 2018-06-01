// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    /// <content>
    /// Contains the nested helper <see cref="Tokens"/> class.
    /// </content>
    internal partial class HttpHeaderParser
    {
        /// <summary>
        /// Contains values from <a href="https://tools.ietf.org/html/rfc822">RF 822</a>.
        /// </summary>
        internal static class Tokens
        {
            /// <summary>
            /// Represents the backslash character.
            /// </summary>
            internal const char Backslash = (char)92;

            /// <summary>
            /// Represents the colon character.
            /// </summary>
            internal const char Colon = (char)58;

            /// <summary>
            /// Represents the carriage return character.
            /// </summary>
            internal const char CR = (char)13;

            /// <summary>
            /// Represents the double quotation character.
            /// </summary>
            internal const char DQuote = (char)34;

            /// <summary>
            /// Represents the horizontal tab character.
            /// </summary>
            internal const char HTab = (char)9;

            /// <summary>
            /// Represents the line feed character.
            /// </summary>
            internal const char LF = (char)10;

            /// <summary>
            /// Represents the space character.
            /// </summary>
            internal const char Space = (char)32;
        }
    }
}
