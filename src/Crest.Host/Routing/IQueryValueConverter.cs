// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    /// <summary>
    /// Allows a query value to be extracted from the url.
    /// </summary>
    internal interface IQueryValueConverter
    {
        /// <summary>
        /// Gets the name of the parameter the converter is for.
        /// </summary>
        string ParameterName { get; }

        /// <summary>
        /// Attempts to convert the specified string into another type.
        /// </summary>
        /// <param name="value">The string value to convert.</param>
        /// <param name="result">
        /// When this method returns, contains the converted value if the
        /// conversion succeeded, or <c>null</c> if the conversion failed.
        /// </param>
        /// <returns>
        /// <c>true</c> if the value was converted; otherwise, <c>false</c>.
        /// </returns>
        bool TryConvertValue(StringSegment value, out object result);
    }
}
