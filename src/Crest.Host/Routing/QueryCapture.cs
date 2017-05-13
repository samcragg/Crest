// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Allows the capturing of values from the URL query.
    /// </summary>
    internal abstract partial class QueryCapture
    {
        private readonly IQueryValueConverter converter;
        private readonly string queryKey;

        private QueryCapture(string queryKey, IQueryValueConverter converter)
        {
            this.converter = converter;
            this.queryKey = queryKey;
        }

        /// <summary>
        /// Creates a <see cref="QueryCapture"/> that can capture the specified
        /// parameter.
        /// </summary>
        /// <param name="queryKey">The name of the query key.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="specializedConverters">
        /// Used to find a specialized converter.
        /// </param>
        /// <returns>A new instance of the <see cref="QueryCapture"/> class.</returns>
        public static QueryCapture Create(
            string queryKey,
            Type parameterType,
            string parameterName,
            IReadOnlyDictionary<Type, Func<string, IQueryValueConverter>> specializedConverters)
        {
            Type elementType = parameterType.IsArray ?
                parameterType.GetElementType() :
                parameterType;

            IQueryValueConverter converter;
            if (specializedConverters.TryGetValue(
                    elementType,
                    out Func<string, IQueryValueConverter> factoryMethod))
            {
                converter = factoryMethod(parameterName);
            }
            else
            {
                converter = new GenericCaptureNode(parameterName, parameterType);
            }

            if (parameterType.IsArray)
            {
                return new MultipleValues(queryKey, elementType, converter);
            }
            else
            {
                return new SingleValue(queryKey, converter);
            }
        }

        /// <summary>
        /// Converts the query values into strongy typed values.
        /// </summary>
        /// <param name="query">Contains the query key/values.</param>
        /// <param name="parameters">Used to store the converted value.</param>
        public abstract void ParseParameters(ILookup<string, string> query, IDictionary<string, object> parameters);
    }
}
