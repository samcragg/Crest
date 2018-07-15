// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

// "IDisposable" should be implemented correctly
// This class would be sealed but is left open to allow it to be mocked for
// unit testing
#pragma warning disable S3881

namespace Crest.Host.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Listens to the file system change notifications for specific files when
    /// they have been written to.
    /// </summary>
    internal class FileWriteWatcher : IDisposable
    {
        private readonly Dictionary<string, Func<Task>> files =
            new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase);

        private readonly FileSystemWatcher watcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileWriteWatcher"/> class.
        /// </summary>
        public FileWriteWatcher()
        {
            this.watcher = new FileSystemWatcher
            {
                NotifyFilter = NotifyFilters.LastWrite,
                IncludeSubdirectories = false,
                Path = AppContext.BaseDirectory,
            };

            this.watcher.Changed += this.OnFileSystemChanged;
            this.watcher.Created += this.OnFileSystemChanged;
            this.watcher.Deleted += this.OnFileSystemChanged;
        }

        /// <summary>
        /// Releases the resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            this.watcher.Dispose();
        }

        /// <summary>
        /// Starts monitoring of file system events.
        /// </summary>
        public virtual void StartMonitoring()
        {
            this.watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Associates the specified callback with changes to the specified
        /// filename.
        /// </summary>
        /// <param name="filename">
        /// The name of the file, relative to the applications base directory.
        /// </param>
        /// <param name="callback">
        /// The action to invoke when a change is raised.
        /// </param>
        public virtual void WatchFile(string filename, Func<Task> callback)
        {
            this.files.Add(filename, callback);
        }

        private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
        {
            if (this.files.TryGetValue(e.Name, out Func<Task> callback))
            {
                Task.Run(callback);
            }
        }
    }
}
