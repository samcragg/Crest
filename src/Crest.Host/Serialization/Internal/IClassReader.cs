// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Internal
{
    using System;

    /// <summary>
    /// Allows the deserializing of types at runtime.
    /// </summary>
    public interface IClassReader
    {
        /// <summary>
        /// Gets the reader to read values from.
        /// </summary>
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
        /// Called before serializing any properties for an instance.
        /// </summary>
        /// <param name="className">The name of the class.</param>
        void ReadBeginClass(string className);

        /// <summary>
        /// Tries to read the name of a property that has been serialized.
        /// </summary>
        /// <returns>
        /// The name of the property, or <c>null</c> if none was read.
        /// </returns>
        string ReadBeginProperty();

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
        /// Called after serializing all the properties for an instance.
        /// </summary>
        void ReadEndClass();

        /// <summary>
        /// Called after serializing a property value.
        /// </summary>
        void ReadEndProperty();
    }
}
