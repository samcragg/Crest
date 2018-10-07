// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using static System.Diagnostics.Debug;

    /// <summary>
    /// Writes a specified stream to the response.
    /// </summary>
    internal sealed class StreamResponse : IResponseData
    {
        // Value copied from https://github.com/dotnet/coreclr/blob/master/src/mscorlib/src/System/IO/Stream.cs#L40:
        // "... the largest multiple of 4096 that is still smaller than the
        //  large object heap threshold (85K)."
        private const int DefaultCopyBufferSize = 81920;
        private readonly Func<Stream, Stream, Task> copyCompressed;
        private readonly Func<Stream> source;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamResponse"/> class.
        /// </summary>
        /// <param name="source">Used to get the stream to send.</param>
        public StreamResponse(Func<Stream> source)
        {
            this.source = source;
            this.copyCompressed = CopyGzipAsync; // This copies the stream without modifying it
            this.Headers = new SortedDictionary<string, string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamResponse"/> class.
        /// </summary>
        /// <param name="source">Used to get the stream to send.</param>
        /// <param name="acceptEncoding">
        /// The encodings accepted by the client for compression.
        /// </param>
        /// <remarks>
        /// The source stream MUST be in the GZip format.
        /// </remarks>
        public StreamResponse(Func<Stream> source, string acceptEncoding)
        {
            this.source = source;
            this.copyCompressed = ChooseCompression(acceptEncoding, out string contentEncoding);

            var headers = new SortedDictionary<string, string>();
            if (contentEncoding != null)
            {
                headers.Add("Content-Encoding", contentEncoding);
            }

            this.Headers = headers;
        }

        /// <inheritdoc />
        public string ContentType { get; set; }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <inheritdoc />
        public int StatusCode => (int)HttpStatusCode.OK;

        /// <inheritdoc />
        public Func<Stream, Task<long>> WriteBody => this.CopyStreamAsync;

        private static Func<Stream, Stream, Task> ChooseCompression(string acceptEncoding, out string contentEncoding)
        {
            foreach (string encoding in acceptEncoding.Split(','))
            {
                string normalized = encoding.Trim().ToUpperInvariant();
                if (normalized == "GZIP")
                {
                    contentEncoding = "GZIP";
                    return CopyGzipAsync;
                }
                else if (normalized == "DEFLATE")
                {
                    contentEncoding = "DEFLATE";
                    return CopyDeflateAsync;
                }
            }

            contentEncoding = null;
            return CopyDecompressedAsync;
        }

        private static async Task CopyDecompressedAsync(Stream source, Stream destination)
        {
            using (var gzip = new GZipStream(source, CompressionMode.Decompress, leaveOpen: true))
            {
                await gzip.CopyToAsync(destination).ConfigureAwait(false);
            }
        }

        private static async Task CopyDeflateAsync(Stream source, Stream destination)
        {
            const int GZipHeaderBytes = 10;
            const int GZipFooterBytes = 8;

            // Skip the GZip header
            source.Position = GZipHeaderBytes;
            int remaining = (int)source.Length - GZipHeaderBytes - GZipFooterBytes;

            byte[] buffer = new byte[DefaultCopyBufferSize];
            while (remaining > 0)
            {
                int bytesRead = await source.ReadAsync(buffer, 0, DefaultCopyBufferSize).ConfigureAwait(false);
                Assert(bytesRead > 0, "Unexpected error reading from the resource stream.");

                if (bytesRead > remaining)
                {
                    bytesRead = remaining;
                }

                await destination.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                remaining -= bytesRead;
            }
        }

        private static Task CopyGzipAsync(Stream source, Stream destination)
        {
            return source.CopyToAsync(destination);
        }

        private async Task<long> CopyStreamAsync(Stream destination)
        {
            using (Stream sourceStream = this.source())
            {
                await this.copyCompressed(sourceStream, destination).ConfigureAwait(false);
                return sourceStream.Position;
            }
        }
    }
}
