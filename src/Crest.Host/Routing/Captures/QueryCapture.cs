﻿// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Captures
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Crest.Core.Logging;

    /// <summary>
    /// Allows the capturing of values from the URL query.
    /// </summary>
    internal abstract partial class QueryCapture
    {
        private static readonly ILog Logger = Log.For<QueryCapture>();
        private readonly IQueryValueConverter converter;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryCapture"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is only used to allow the type to be mocked in unit tests.
        /// </remarks>
        protected QueryCapture()
        {
        }

        private QueryCapture(string parameterName, IQueryValueConverter converter)
        {
            this.converter = converter;
            this.ParameterName = parameterName;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <param name="value">Outputs the value, if any, to this parameter.</param>
        /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
        public delegate bool TryGetValue(Type key, out Func<string, IQueryValueConverter> value);

        /// <summary>
        /// Gets the name of the parameter for the captured value.
        /// </summary>
        internal string ParameterName { get; }

        /// <summary>
        /// Creates a <see cref="QueryCapture"/> that can capture the specified
        /// parameter.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <param name="getConverter">
        /// Used to find a specialized converter.
        /// </param>
        /// <returns>A new instance of the <see cref="QueryCapture"/> class.</returns>
        public static QueryCapture Create(
            string parameterName,
            Type parameterType,
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
                return new MultipleValues(parameterName, elementType, valueConverter);
            }
            else
            {
                return new SingleValue(parameterName, valueConverter);
            }
        }

        /// <summary>
        /// Creates a <see cref="QueryCapture"/> that can capture all the
        /// non-captured key/values of the query into a single parameter.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <returns>A new instance of the <see cref="QueryCapture"/> class.</returns>
        public static QueryCapture CreateCatchAll(string parameterName)
        {
            return new CatchAll(parameterName);
        }

        /// <summary>
        /// Converts the query values into strongly typed values.
        /// </summary>
        /// <param name="query">Contains the query key/values.</param>
        /// <param name="parameters">Used to store the converted value.</param>
        public abstract void ParseParameters(ILookup<string, string> query, IDictionary<string, object> parameters);
    }
}
