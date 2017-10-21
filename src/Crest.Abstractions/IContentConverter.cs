// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System;
    using System.Collections.Generic;
    using System.IO;

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
        /// Gets the supported MIME types, optionally with their quality.
        /// </summary>
        /// <remarks>
        /// Although parameters are ignored, the quality parameter will be
        /// considered when matching (i.e. "text/plain;q=0.8" would be
        /// considered after another converter that has a higher (or none
        /// specified) quality.
        /// </remarks>
        IEnumerable<string> Formats { get; }

        /// <summary>
        /// Gets the priority of this converter when multiple converters can
        /// match the same media type with the same quality.
        /// </summary>
        /// <remarks>
        /// Higher priority (bigger values) should take precedence over
        /// converters that match with a lower priority (smaller value).
        /// </remarks>
        int Priority { get; }

        /// <summary>
        /// Allows optimizations to be performed during startup for a type that
        /// is expected to be returned later.
        /// </summary>
        /// <param name="type">The type of the return value.</param>
        void Prime(Type type);

        /// <summary>
        /// Writes the specified object to the stream.
        /// </summary>
        /// <param name="stream">Where to write the object to.</param>
        /// <param name="obj">The object to write.</param>
        void WriteTo(Stream stream, object obj);
    }
}
