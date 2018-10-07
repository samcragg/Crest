// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.IO
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the reading of files.
    /// </summary>
    internal class FileReader
    {
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

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
        /// Reads the entire contents of a file into a string.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The value of the
        /// <c>TResult</c> parameter is a string containing the contents of the
        /// file.
        /// </returns>
        public virtual async Task<string> ReadAllTextAsync(string path)
        {
            using (Stream file = this.OpenFile(path))
            using (var reader = new StreamReader(file, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Opens a file in asynchronous mode.
        /// </summary>
        /// <param name="path">The file to open for reading.</param>
        /// <returns>The file stream.</returns>
        protected virtual Stream OpenFile(string path)
        {
            return new FileStream(
                ValidatePath(path),
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);
        }

        private static string ValidatePath(string path)
        {
            if (path.IndexOfAny(InvalidFileNameChars) >= 0)
            {
                throw new InvalidOperationException("Path contains directory information.");
            }

            return path;
        }
    }
}
