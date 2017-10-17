﻿// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;

    /// <summary>
    /// Allows the serialization of a type.
    /// </summary>
    public interface ITypeSerializer
    {
        /// <summary>
        /// Clears all internal buffers, causing any buffered data to be
        /// written to the underlying stream.
        /// </summary>
        void Flush();

        /// <summary>
        /// Writes the specified object to the underlying stream.
        /// </summary>
        /// <param name="instance">The instance to serialize.</param>
        void Write(object instance);

        /// <summary>
        /// Writes the specified array to the underlying stream.
        /// </summary>
        /// <param name="array">The array to serialize.</param>
        void WriteArray(Array array);
    }
}
