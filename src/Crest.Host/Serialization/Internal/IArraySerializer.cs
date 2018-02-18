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
    [CLSCompliant(false)]
    public interface IArraySerializer
    {
        /// <summary>
        /// Gets the writer to output values to.
        /// </summary>
        IStreamWriter Writer { get; }

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
