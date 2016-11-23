// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    /// <summary>
    /// Creates a <see cref="IContentConverter"/> based on the request information.
    /// </summary>
    public interface IContentConverterFactory
    {
        /// <summary>
        /// Gets a content converter for the specified accept header.
        /// </summary>
        /// <param name="accept">The value of the requests accept header.</param>
        /// <returns>
        /// A content converter that can be used to serialize the reply or
        /// <c>null</c> if none was found.
        /// </returns>
        IContentConverter GetConverter(string accept);
    }
}
