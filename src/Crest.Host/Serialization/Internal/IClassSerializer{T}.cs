// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Internal
{
    /// <summary>
    /// Allows the serializing of types at runtime.
    /// </summary>
    /// <typeparam name="T">The type of metadata to store about properties.</typeparam>
    public interface IClassSerializer<in T> : IClassReader, IClassWriter, IPrimitiveSerializer<T>
        where T : class
    {
        /// <summary>
        /// Called before serializing any properties for an instance.
        /// </summary>
        /// <param name="metadata">The metadata for the class.</param>
        void ReadBeginClass(T metadata);

        /// <summary>
        /// Called before serializing any properties for an instance.
        /// </summary>
        /// <param name="metadata">The metadata for the class.</param>
        void WriteBeginClass(T metadata);

        /// <summary>
        /// Called before serializing a property value.
        /// </summary>
        /// <param name="propertyMetadata">The metadata for the property.</param>
        void WriteBeginProperty(T propertyMetadata);
    }
}
