// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents information about an uploaded file.
    /// </summary>
    public interface IFileData
    {
        /// <summary>
        /// Gets the raw contents for the file.
        /// </summary>
        byte[] Contents { get; }

        /// <summary>
        /// Gets a value that indicates the media type of the file.
        /// </summary>
        /// <remarks>
        /// The indicated media type defines both the data format and how that
        /// data is intended to be processed by a recipient.
        /// </remarks>
        string ContentType { get; }

        /// <summary>
        /// Gets the name of the uploaded file.
        /// </summary>
        /// <remarks>
        /// <a href="https://tools.ietf.org/html/rfc7578">RFC7578</a>:
        /// Do not use the file name blindly, check and possibly change to
        /// match local file system conventions if applicable, and do not use
        /// directory path information that may be present.
        /// </remarks>
        string Filename { get; }

        /// <summary>
        /// Gets the headers sent with the file.
        /// </summary>
        IReadOnlyDictionary<string, string> Headers { get; }
    }
}
