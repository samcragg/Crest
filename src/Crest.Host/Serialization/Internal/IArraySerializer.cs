// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Internal
{
    using System;

    /// <summary>
    /// Allows the serialization of arrays to the response stream.
    /// </summary>
    public interface IArraySerializer
    {
        /// <summary>
        /// Gets the reader.
        /// </summary>
        // HACK: Remove this property when IPrimitiveSerializer has been upgraded
        ValueReader Reader { get; }

        /// <summary>
        /// Called before reading an array value.
        /// </summary>
        /// <param name="elementType">The type of the array element.</param>
        /// <returns>
        /// <c>true</c> if there are further array elements to read; otherwise,
        /// <c>false</c>.
        /// </returns>
        bool ReadBeginArray(Type elementType);

        /// <summary>
        /// Called after reading an element to determine whether there are more
        /// elements to follow.
        /// </summary>
        /// <returns>
        /// <c>true</c> if there are further array elements to read; otherwise,
        /// <c>false</c>.
        /// </returns>
        bool ReadElementSeparator();

        /// <summary>
        /// Called after reading an array value.
        /// </summary>
        /// <remarks>
        /// This method is only called if <see cref="ReadBeginArray(Type)"/>
        /// returned <c>true</c>.
        /// </remarks>
        void ReadEndArray();

        /// <summary>
        /// Called before writing an array value.
        /// </summary>
        /// <param name="elementType">The type of the array element.</param>
        /// <param name="size">The length of the array.</param>
        void WriteBeginArray(Type elementType, int size);

        /// <summary>
        /// Called after writing an element if there are more elements to follow.
        /// </summary>
        void WriteElementSeparator();

        /// <summary>
        /// Called after writing an array value.
        /// </summary>
        void WriteEndArray();
    }
}
