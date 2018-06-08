﻿// Copyright (c) Samuel Cragg.
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
    /// <typeparam name="TBase">The type of the base class.</typeparam>
    internal interface ISerializerGenerator<TBase>
    {
        /// <summary>
        /// Deserializes the specified type from the stream.
        /// </summary>
        /// <param name="stream">The input to deserialized value.</param>
        /// <param name="type">The type to deserialize.</param>
        /// <returns>The deserialized object.</returns>
        object Deserialize(Stream stream, Type type);

        /// <summary>
        /// Gets a serializer for the specific type, generating a type if one
        /// doesn't already exist.
        /// </summary>
        /// <param name="classType">The class to serialize.</param>
        /// <returns>A type implementing <see cref="ITypeSerializer"/>.</returns>
        Type GetSerializerFor(Type classType);

        /// <summary>
        /// Serializes the specified value to the stream.
        /// </summary>
        /// <param name="stream">The output for the serialized value.</param>
        /// <param name="value">The value to serialize.</param>
        void Serialize(Stream stream, object value);
    }
}
