// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the conversion of an object to/from a specific format.
    /// </summary>
    public interface IContentConverter
    {
        /// <summary>
        /// Gets the MIME type of the serialized content.
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Gets the MIME types supported.
        /// </summary>
        IReadOnlyList<string> Formats { get; }

        /// <summary>
        /// Writes the specified object to the stream.
        /// </summary>
        /// <param name="stream">Where to write the object to.</param>
        /// <param name="obj">The object to write.</param>
        /// <returns>The result of the asynchronous operation.</returns>
        Task WriteToAsync(Stream stream, object obj);
    }
}
