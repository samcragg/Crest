// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization
{
    using System;
    using System.IO;
    using Crest.Host.Serialization.Internal;

    /// <summary>
    /// Generates serializers at runtime for specific types.
    /// </summary>
    /// <typeparam name="T">The type of the formatter class.</typeparam>
    internal interface ISerializerGenerator<T>
        where T : IFormatter
    {
        /// <summary>
        /// Deserializes the specified type from the stream.
        /// </summary>
        /// <param name="stream">The input to deserialized value.</param>
        /// <param name="type">The type to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        object Deserialize(Stream stream, Type type);

        /// <summary>
        /// Ensures a serializer for the specific type exists.
        /// </summary>
        /// <param name="classType">The class to serialize.</param>
        void Prime(Type classType);

        /// <summary>
        /// Serializes the specified value to the stream.
        /// </summary>
        /// <param name="stream">The output for the serialized value.</param>
        /// <param name="value">The value to serialize.</param>
        void Serialize(Stream stream, object value);
    }
}
