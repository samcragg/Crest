// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Internal
{
    using System;

    /// <summary>
    /// Allows the serializing of types at runtime.
    /// </summary>
    public interface IClassWriter
    {
        /// <summary>
        /// Gets the writer to output values to.
        /// </summary>
        ValueWriter Writer { get; }

        /// <summary>
        /// Called before writing an array value.
        /// </summary>
        /// <param name="elementType">The type of the array element.</param>
        /// <param name="size">The length of the array.</param>
        void WriteBeginArray(Type elementType, int size);

        /// <summary>
        /// Called before serializing any properties for an instance.
        /// </summary>
        /// <param name="className">The name of the class being written.</param>
        void WriteBeginClass(string className);

        /// <summary>
        /// Called before serializing a property value.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        void WriteBeginProperty(string propertyName);

        /// <summary>
        /// Called after writing an element if there are more elements to follow.
        /// </summary>
        void WriteElementSeparator();

        /// <summary>
        /// Called after writing an array value.
        /// </summary>
        void WriteEndArray();

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
