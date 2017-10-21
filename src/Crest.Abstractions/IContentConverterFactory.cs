// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Abstractions
{
    using System;

    /// <summary>
    /// Creates a <see cref="IContentConverter"/> based on the request information.
    /// </summary>
    public interface IContentConverterFactory
    {
        /// <summary>
        /// Gets a content converter for the specified accept header.
        /// </summary>
        /// <param name="accept">The value of the requests accept header.</param>
        /// <returns>
        /// A content converter that can be used to serialize the reply or
        /// <c>null</c> if none was found.
        /// </returns>
        IContentConverter GetConverter(string accept);

        /// <summary>
        /// Notifies the converters of a type that is expected to be returned.
        /// </summary>
        /// <param name="type">The type of the return value.</param>
        void PrimeConverters(Type type);
    }
}
