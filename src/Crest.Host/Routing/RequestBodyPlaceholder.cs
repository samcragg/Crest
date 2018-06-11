// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
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
        public object Value { get; private set; }

        /// <summary>
        /// Updates the parameters in the request with the value in the body.
        /// </summary>
        /// <param name="converterFactory">
        /// Used to get the converter for the request data.
        /// </param>
        /// <param name="streamPool">Used to obtain temporary streams.</param>
        /// <param name="request">Contains the request data.</param>
        /// <returns>
        /// <c>true</c> if the conversion was successful; otherwise, <c>false</c>.
        /// </returns>
        internal async Task<bool> UpdateRequest(
            IContentConverterFactory converterFactory,
            BlockStreamPool streamPool,
            IRequestData request)
        {
            // TODO:
            await Task.Yield();
            return false;
        }
    }
}
