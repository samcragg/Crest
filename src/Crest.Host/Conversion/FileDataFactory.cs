// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Crest.Abstractions;
    using Crest.Core;
    using Crest.Host.IO;
    using Crest.Host.Logging;

    /// <summary>
    /// Creates objects representing the uploaded files in a request.
    /// </summary>
    internal sealed partial class FileDataFactory : IContentConverter
    {
        private const string ContentDispositionHeader = "Content-Disposition";
        private const string ContentTypeHeader = "Content-Type";
        private const string DefaultContentType = "text/plain"; // RFC7578 §4.4
        private static readonly ILog Logger = LogProvider.For<FileDataFactory>();
        private readonly ArrayPool<byte> bytePool;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileDataFactory"/> class.
        /// </summary>
        /// <param name="bytePool">Used to obtain temporary byte buffers.</param>
        public FileDataFactory(ArrayPool<byte> bytePool)
        {
            this.bytePool = bytePool;
        }

        /// <inheritdoc />
        public bool CanRead => true;

        /// <inheritdoc />
        public bool CanWrite => false;

        /// <inheritdoc />
        public string ContentType => throw new NotSupportedException();

        /// <inheritdoc />
        public IEnumerable<string> Formats
        {
            get
            {
                yield return "multipart/*";
            }
        }

        /// <inheritdoc />
        public int Priority => 600;

        /// <inheritdoc />
        public void Prime(Type type)
        {
        }

        /// <inheritdoc />
        public object ReadFrom(IReadOnlyDictionary<string, string> headers, Stream stream, Type type)
        {
            IFileData[] files = this.CreateFiles(headers, stream);

            // Handle the scenario where the method parameter is IEnumerable<IFileData>
            if (type.IsAssignableFrom(typeof(IFileData[])))
            {
                return files;
            }
            else if (files.Length > 0)
            {
                if (files.Length > 1)
                {
                    Logger.Info("Multiple files have been sent, however, only the first file has been used for processing.");
                }

                return files[0];
            }
            else
            {
                return null;
            }
        }

        /// <inheritdoc />
        public void WriteTo(Stream stream, object obj)
        {
            throw new NotSupportedException();
        }

        private static string FindParameter(HttpHeaderParser parser, string parameter)
        {
            while (parser.ReadCharacter(';'))
            {
                if (parser.ReadParameter(out string attribute, out string value) &&
                    string.Equals(attribute, parameter, StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }
            }

            return null;
        }

        private static string ParseBoundary(IReadOnlyDictionary<string, string> headers)
        {
            // RFC 2045
            // content := "Content-Type" ":" type "/" subtype
            //             *(";" parameter)
            if (headers.TryGetValue(ContentTypeHeader, out string contentType))
            {
                var parser = new HttpHeaderParser(contentType);
                if (ReadMultipartTypeAndSubtype(parser))
                {
                    return FindParameter(parser, "boundary");
                }
            }

            return null;
        }

        private static string ParseFilename(IReadOnlyDictionary<string, string> headers)
        {
            // RFC 2183
            // disposition := "Content-Disposition" ":"
            //                disposition-type
            //                *(";" disposition-parm)
            if (headers.TryGetValue(ContentDispositionHeader, out string disposition))
            {
                var parser = new HttpHeaderParser(disposition);
                if (ReadDispositionType(parser))
                {
                    return FindParameter(parser, "filename");
                }
            }

            return null;
        }

        private static bool ReadDispositionType(HttpHeaderParser parser)
        {
            // disposition-type := "inline"
            //                   / "attachment"
            //                   / extension-token
            //                   ; values are not case-sensitive
            //
            // extension-token := ietf-token / x-token
            //
            // ietf-token := <An extension token defined by a standards-track
            //                RFC and registered with IANA.>
            //
            // x-token := <The two characters "X-" or "x-" followed, with no
            //             intervening white space, by any token>
            //
            // Since we're not using this, just parse it as a token
            return parser.ReadToken(out _);
        }

        private static bool ReadMultipartTypeAndSubtype(HttpHeaderParser parser)
        {
            // type := discrete-type / composite-type
            //
            // composite-type := "message" / "multipart" / extension-token
            //
            // subtype := extension-token / iana-token
            //
            // Matching of media type and subtype is ALWAYS case-insensitive.
            return parser.ReadToken(out string type) &&
                   string.Equals(type, "multipart", StringComparison.OrdinalIgnoreCase) &&
                   parser.ReadCharacter('/') &&
                   parser.ReadToken(out _);
        }

        private IFileData ConvertPart(Stream body, MultipartParser.BodyPart part)
        {
            IReadOnlyDictionary<string, string> headers =
                this.ReadHeaders(body, part.HeaderStart, part.HeaderEnd);

            // We can't use the pool here as we're handing the byte array over
            // so can't control its lifetime plus it needs to be the correct
            // size (i.e. we can't enforce the user uses a certain part of it)
            int length = part.BodyEnd - part.BodyStart;
            byte[] data = new byte[length];
            body.Position = part.BodyStart;
            IOUtils.ReadBytes(body, data, length);

            return new FileData(data, headers);
        }

        private IFileData[] CreateFiles(IReadOnlyDictionary<string, string> headers, Stream body)
        {
            string boundary = ParseBoundary(headers);
            if (boundary == null)
            {
                Logger.Warn("No boundary parameter found.");
                return new IFileData[0];
            }

            // We need to call ToList as the Parse method is lazy and uses the
            // same stream that we use when converting the parts and since we
            // change the position of the stream, that will upset the parser
            var parser = new MultipartParser(boundary, body);
            List<MultipartParser.BodyPart> parts = parser.Parse().ToList();

            var files = new IFileData[parts.Count];
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = this.ConvertPart(body, parts[i]);
            }

            return files;
        }

        private IReadOnlyDictionary<string, string> ReadHeaders(Stream body, int start, int end)
        {
            int length = end - start;
            byte[] headerBytes = this.bytePool.Rent(length);

            try
            {
                body.Position = start;
                length = IOUtils.ReadBytes(body, headerBytes, length);

                var parser = new HttpHeaderParser(new ByteIterator(headerBytes, length));
                return parser.ReadPairs();
            }
            finally
            {
                this.bytePool.Return(headerBytes);
            }
        }
    }
}
