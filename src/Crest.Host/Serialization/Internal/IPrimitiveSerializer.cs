// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Internal
{
    using System;

    /// <summary>
    /// Allows the serialization of a primitive type to the response stream.
    /// </summary>
    /// <typeparam name="T">The type of metadata to store about properties.</typeparam>
    public interface IPrimitiveSerializer<T>
        where T : class
    {
        /// <summary>
        /// Gets the writer to output values to.
        /// </summary>
        ValueWriter Writer { get; }

        /// <summary>
        /// Called before writing the primitive value.
        /// </summary>
        /// <param name="metadata">The metadata for the type to be written.</param>
        void BeginWrite(T metadata);

        /// <summary>
        /// Called after writing the primitive value.
        /// </summary>
        void EndWrite();

        /// <summary>
        /// Clears all internal buffers, causing any buffered data to be
        /// written to the underlying stream.
        /// </summary>
        void Flush();
    }
}
