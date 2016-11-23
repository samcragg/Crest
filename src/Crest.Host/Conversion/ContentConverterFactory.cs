// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Creates a <see cref="IContentConverter"/> based on the request information.
    /// </summary>
    internal sealed class ContentConverterFactory : IContentConverterFactory
    {
        private readonly IContentConverter converter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentConverterFactory"/> class.
        /// </summary>
        /// <param name="converters">The available converters.</param>
        public ContentConverterFactory(IEnumerable<IContentConverter> converters)
        {
            this.converter = converters.FirstOrDefault();
        }

        /// <inheritdoc />
        public IContentConverter GetConverter(string accept)
        {
            return this.converter;
        }
    }
}