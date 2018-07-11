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
    public interface IClassSerializer<in T> : IArraySerializer, IPrimitiveSerializer<T>
        where T : class
    {
        /// <summary>
        /// Called before serializing any properties for an instance.
        /// </summary>
        /// <param name="metadata">The metadata for the class.</param>
        void ReadBeginClass(T metadata);

        /// <summary>
        /// Tries to read the name of a property that has been serialized.
        /// </summary>
        /// <returns>
        /// The name of the property, or <c>null</c> if none was read.
        /// </returns>
        string ReadBeginProperty();

        /// <summary>
        /// Called after serializing all the properties for an instance.
        /// </summary>
        void ReadEndClass();

        /// <summary>
        /// Called after serializing a property value.
        /// </summary>
        void ReadEndProperty();

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

        /// <summary>
        /// Called after serializing all the properties for an instance.
        /// </summary>
        void WriteEndClass();

        /// <summary>
        /// Called after serializing a property value.
        /// </summary>
        void WriteEndProperty();
    }
}
