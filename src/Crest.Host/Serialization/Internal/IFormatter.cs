// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Internal
{
    /// <summary>
    /// Allows the reading/writing of objects.
    /// </summary>
    public interface IFormatter : IClassReader, IClassWriter
    {
        /// <summary>
        /// Gets a value indicating whether enumerations should be written as
        /// their integer values or not.
        /// </summary>
        bool EnumsAsIntegers { get; }

        /// <summary>
        /// Called before deserializing any properties of an instance.
        /// </summary>
        /// <param name="metadata">The metadata for the class being read.</param>
        void ReadBeginClass(object metadata);

        /// <summary>
        /// Called before reading a primitive value.
        /// </summary>
        /// <param name="metadata">The metadata for the primitive being read.</param>
        void ReadBeginPrimitive(object metadata);

        /// <summary>
        /// Called after reading the primitive value.
        /// </summary>
        void ReadEndPrimitive();

        /// <summary>
        /// Called before serializing any properties for an instance.
        /// </summary>
        /// <param name="metadata">The metadata for the class being written.</param>
        void WriteBeginClass(object metadata);

        /// <summary>
        /// Called before writing a primitive value.
        /// </summary>
        /// <param name="metadata">The metadata for the primitive being written.</param>
        void WriteBeginPrimitive(object metadata);

        /// <summary>
        /// Called before serializing a property value.
        /// </summary>
        /// <param name="metadata">The metadata for the property.</param>
        void WriteBeginProperty(object metadata);

        /// <summary>
        /// Called after writing the primitive value.
        /// </summary>
        void WriteEndPrimitive();
    }
}
