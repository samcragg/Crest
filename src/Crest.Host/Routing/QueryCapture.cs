// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Host.Logging;

    /// <summary>
    /// Allows the capturing of values from the URL query.
    /// </summary>
    internal abstract partial class QueryCapture
    {
        private static readonly ILog Logger = LogProvider.For<QueryCapture>();
        private readonly IQueryValueConverter converter;
        private readonly string queryKey;

        private QueryCapture(string queryKey, IQueryValueConverter converter)
        {
            this.converter = converter;
            this.queryKey = queryKey;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <param name="value">Outputs the value, if any, to this parameter.</param>
        /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
        public delegate bool TryGetValue(Type key, out Func<string, IQueryValueConverter> value);

        /// <summary>
        /// Creates a <see cref="QueryCapture"/> that can capture the specified
        /// parameter.
        /// </summary>
        /// <param name="queryKey">The name of the query key.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="getConverter">
        /// Used to find a specialized converter.
        /// </param>
        /// <returns>A new instance of the <see cref="QueryCapture"/> class.</returns>
        public static QueryCapture Create(
            string queryKey,
            Type parameterType,
            string parameterName,
            TryGetValue getConverter)
        {
            Type elementType = parameterType.IsArray ?
                parameterType.GetElementType() :
                parameterType;

            IQueryValueConverter valueConverter;
            if (getConverter(elementType, out Func<string, IQueryValueConverter> factoryMethod))
            {
                valueConverter = factoryMethod(parameterName);
            }
            else
            {
                valueConverter = new GenericCaptureNode(parameterName, parameterType);
            }

            if (parameterType.IsArray)
            {
                return new MultipleValues(queryKey, elementType, valueConverter);
            }
            else
            {
                return new SingleValue(queryKey, valueConverter);
            }
        }

        /// <summary>
        /// Converts the query values into strongly typed values.
        /// </summary>
        /// <param name="query">Contains the query key/values.</param>
        /// <param name="parameters">Used to store the converted value.</param>
        public abstract void ParseParameters(ILookup<string, string> query, IDictionary<string, object> parameters);
    }
}
