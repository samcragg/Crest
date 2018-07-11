// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Internal
{
    /// <summary>
    /// Allows the serialization of a primitive type to/from a stream.
    /// </summary>
    /// <typeparam name="T">The type of metadata to store about properties.</typeparam>
    public interface IPrimitiveSerializer<in T>
        where T : class
    {
        /// <summary>
        /// Gets the reader to read values from.
        /// </summary>
        ValueReader Reader { get; }

        /// <summary>
        /// Gets the writer to output values to.
        /// </summary>
        ValueWriter Writer { get; }

        /// <summary>
        /// Called before reading the primitive value.
        /// </summary>
        /// <param name="metadata">The metadata for the type to be read.</param>
        void BeginRead(T metadata);

        /// <summary>
        /// Called before writing the primitive value.
        /// </summary>
        /// <param name="metadata">The metadata for the type to be written.</param>
        void BeginWrite(T metadata);

        /// <summary>
        /// Called after reading the primitive value.
        /// </summary>
        void EndRead();

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
