// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.DataAccess.Parsing
{
    using System.Reflection;

    /// <summary>
    /// Represents information about filtering data.
    /// </summary>
    internal readonly struct FilterInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterInfo"/> struct.
        /// </summary>
        /// <param name="property">The property to filter.</param>
        /// <param name="method">The filter method.</param>
        /// <param name="value">The filter value.</param>
        public FilterInfo(PropertyInfo property, FilterMethod method, string value)
        {
            this.Method = method;
            this.Property = property;
            this.Value = value;
        }

        /// <summary>
        /// Gets the type of method to apply to filter.
        /// </summary>
        public FilterMethod Method { get; }

        /// <summary>
        /// Gets the property to filter on.
        /// </summary>
        public PropertyInfo Property { get; }

        /// <summary>
        /// Gets the value to apply to the filter method.
        /// </summary>
        public string Value { get; }
    }
}
