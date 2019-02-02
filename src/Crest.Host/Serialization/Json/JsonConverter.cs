// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Serialization.Json
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Crest.Abstractions;
    using Crest.Host.Serialization;

    /// <summary>
    /// Converts between .NET objects and JSON.
    /// </summary>
    internal sealed class JsonConverter : IContentConverter
    {
        private const string JsonMimeType = @"application/json";
        private readonly ISerializerGenerator<JsonFormatter> generator;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonConverter"/> class.
        /// </summary>
        /// <param name="generator">Used to generate the serializers.</param>
        public JsonConverter(ISerializerGenerator<JsonFormatter> generator)
        {
            this.generator = generator;
        }

        /// <inheritdoc />
        public bool CanRead => true;

        /// <inheritdoc />
        public bool CanWrite => true;

        /// <inheritdoc />
        public string ContentType => JsonMimeType;

        /// <inheritdoc />
        public IEnumerable<string> Formats
        {
            get
            {
                yield return JsonMimeType;
            }
        }

        /// <inheritdoc />
        public int Priority => 500;

        /// <inheritdoc />
        public void Prime(Type type)
        {
            this.generator.Prime(type);
        }

        /// <inheritdoc />
        public object ReadFrom(IReadOnlyDictionary<string, string> headers, Stream stream, Type type)
        {
            return this.generator.Deserialize(stream, type);
        }

        /// <inheritdoc />
        public void WriteTo(Stream stream, object value)
        {
            this.generator.Serialize(stream, value);
        }
    }
}
