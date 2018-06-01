// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System.Collections.Generic;
    using Crest.Core;

    /// <content>
    /// Contains the nested <see cref="FileData"/> class.
    /// </content>
    internal partial class FileDataFactory
    {
        private class FileData : IFileData
        {
            internal FileData(byte[] data, IReadOnlyDictionary<string, string> headers)
            {
                headers.TryGetValue(ContentTypeHeader, out string contentType);

                this.Contents = data;
                this.ContentType = contentType ?? DefaultContentType;
                this.Headers = headers;
                this.Filename = ParseFilename(headers);
            }

            /// <inheritdoc />
            public byte[] Contents { get; }

            /// <inheritdoc />
            public string ContentType { get; }

            /// <inheritdoc />
            public string Filename { get; }

            /// <inheritdoc />
            public IReadOnlyDictionary<string, string> Headers { get; }
        }
    }
}
