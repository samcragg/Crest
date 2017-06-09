// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Crest.Abstractions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Converts between .NET objects and JSON
    /// </summary>
    internal sealed class JsonConverter : IContentConverter
    {
        private const string JsonMimeType = @"application/json";

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
        public void WriteTo(Stream stream, object obj)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 4096, leaveOpen: true))
            {
                var serializer = JsonSerializer.CreateDefault();
                serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
                serializer.Serialize(writer, obj);
            }
        }
    }
}
