// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.IO
{
    using System.IO;

    /// <summary>
    /// Provides utility methods for working with IO operations.
    /// </summary>
    internal static class IOUtils
    {
        /// <summary>
        /// Reads the specified number of bytes from the stream.
        /// </summary>
        /// <param name="stream">The stream to read the data from.</param>
        /// <param name="buffer">Where to read the data to.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes read.</returns>
        internal static int ReadBytes(Stream stream, byte[] buffer, int count)
        {
            int offset = 0;
            do
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read == 0)
                {
                    break;
                }

                offset += read;
            }
            while (offset < count);

            return offset;
        }
    }
}