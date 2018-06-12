// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Host.IO;

    /// <summary>
    /// Allows the request body to be injected into a parameter.
    /// </summary>
    internal class RequestBodyPlaceholder : IValueProvider
    {
        private readonly Type type;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestBodyPlaceholder"/> class.
        /// </summary>
        /// <param name="type">The type of the parameter.</param>
        public RequestBodyPlaceholder(Type type)
        {
            this.type = type;
        }

        /// <inheritdoc />
        public virtual object Value { get; private set; }

        /// <summary>
        /// Updates the parameters in the request with the value in the body.
        /// </summary>
        /// <param name="converter">Used to convert the request data.</param>
        /// <param name="streamPool">Used to obtain temporary streams.</param>
        /// <param name="request">Contains the request data.</param>
        /// <returns>
        /// <c>true</c> if the conversion was successful; otherwise, <c>false</c>.
        /// </returns>
        internal virtual async Task<bool> UpdateRequestAsync(
            IContentConverter converter,
            BlockStreamPool streamPool,
            IRequestData request)
        {
            if (request.Body.CanSeek)
            {
                this.UpdateValue(converter, request.Headers, request.Body);
            }
            else
            {
                using (Stream buffer = streamPool.GetStream())
                {
                    await request.Body.CopyToAsync(buffer).ConfigureAwait(false);
                    buffer.Position = 0;
                    this.UpdateValue(converter, request.Headers, buffer);
                }
            }

            return true;
        }

        private void UpdateValue(
            IContentConverter converter,
            IReadOnlyDictionary<string, string> headers,
            Stream stream)
        {
            this.Value = converter.ReadFrom(headers, stream, this.type);
        }
    }
}
