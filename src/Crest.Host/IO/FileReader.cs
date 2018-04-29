// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.IO
{
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the reading of files.
    /// </summary>
    internal class FileReader
    {
        /// <summary>
        /// Reads the entire contents of a file into a byte array.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter is a byte array containing the contents of
        /// the file.
        /// </returns>
        public virtual async Task<byte[]> ReadAllBytesAsync(string path)
        {
            using (Stream file = this.OpenFile(path))
            {
                int index = 0;
                int count = (int)file.Length;
                byte[] bytes = new byte[count];
                while (count > 0)
                {
                    int read = await file.ReadAsync(bytes, index, count).ConfigureAwait(false);
                    if (read == 0)
                    {
                        throw new EndOfStreamException();
                    }

                    index += read;
                    count -= read;
                }

                return bytes;
            }
        }

        /// <summary>
        /// Opens a file in asynchronous mode.
        /// </summary>
        /// <param name="path">A relative or absolute path for the file.</param>
        /// <returns>The file stream.</returns>
        protected virtual Stream OpenFile(string path)
        {
            return new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
        }
    }
}
