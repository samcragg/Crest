// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host
{
    /// <summary>
    /// Represents the result of a handler method that does not return a value.
    /// </summary>
    internal sealed class NoContent
    {
        /// <summary>
        /// Represents the sole instance of the <see cref="NoContent"/> class.
        /// </summary>
        internal static readonly NoContent Value = new NoContent();

        private NoContent()
        {
        }
    }
}
