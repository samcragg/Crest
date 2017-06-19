// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Crest.Abstractions;

    /// <summary>
    /// Indicates to the sender that the resource has moved to another location.
    /// </summary>
    internal sealed class RedirectResponse : IResponseData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedirectResponse"/> class.
        /// </summary>
        /// <param name="redirectRoute">The Uri to redirect to.</param>
        public RedirectResponse(string redirectRoute)
        {
            this.Headers = new SortedDictionary<string, string>
            {
                { "Location", redirectRoute }
            };
        }

        /// <inheritdoc />
        public string ContentType => string.Empty;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <inheritdoc />
        public int StatusCode => (int)HttpStatusCode.MovedPermanently;

        /// <inheritdoc />
        public Func<Stream, Task> WriteBody => _ => Task.CompletedTask;
    }
}
