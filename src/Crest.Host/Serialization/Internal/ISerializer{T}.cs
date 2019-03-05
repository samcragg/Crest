// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Internal
{
    /// <summary>
    /// Allows the serialization of a type.
    /// </summary>
    /// <typeparam name="T">The type for the custom serialization.</typeparam>
    public interface ISerializer<T>
    {
        /// <summary>
        /// Reads an object from specified reader.
        /// </summary>
        /// <param name="reader">Used to read the raw data from.</param>
        /// <returns>A new object containing the read information.</returns>
        T Read(IClassReader reader);

        /// <summary>
        /// Writes the specified object to the specified writer.
        /// </summary>
        /// <param name="writer">Used to write the raw data to.</param>
        /// <param name="instance">The instance to serialize.</param>
        void Write(IClassWriter writer, T instance);
    }
}
