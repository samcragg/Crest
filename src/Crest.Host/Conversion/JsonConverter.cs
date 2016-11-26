// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Converts between .NET objects and JSON
    /// </summary>
    internal sealed class JsonConverter : IContentConverter
    {
        private const string JsonMimeType = @"application/json";

        /// <inheritdoc />
        public string ContentType
        {
            get { return JsonMimeType; }
        }

        /// <inheritdoc />
        public IEnumerable<string> Formats
        {
            get
            {
                yield return JsonMimeType;
            }
        }

        /// <inheritdoc />
        public int Priority
        {
            get { return 500; }
        }

        /// <inheritdoc />
        public async Task WriteToAsync(Stream stream, object obj)
        {
            // Write to a memory stream first as JsonSerializer doesn't do async
            using (var memory = new MemoryStream())
            using (var writer = new StreamWriter(memory, Encoding.UTF8))
            {
                JsonSerializer serializer = JsonSerializer.CreateDefault();
                serializer.ContractResolver = new CamelCasePropertyNamesContractResolver();
                serializer.Serialize(writer, obj);
                writer.Flush();

                // Can't return the CopyToAsync Task as the memory stream will
                // get disposed so task it up...
                memory.Position = 0;
                await memory.CopyToAsync(stream).ConfigureAwait(false);
            }
        }
    }
}
