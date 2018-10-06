// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    /// <content>
    /// Contains the nested <see cref="ErrorType"/> enum.
    /// </content>
    internal partial class UrlParser
    {
        /// <summary>
        /// Represents the error found during parsing.
        /// </summary>
        protected enum ErrorType
        {
            /// <summary>
            /// Indicates a parameter has been marked as FromBody, however,
            /// appears as a capture.
            /// </summary>
            CannotBeMarkedAsFromBody,

            /// <summary>
            /// Indicates a parameter is captured multiple times.
            /// </summary>
            DuplicateParameter,

            /// <summary>
            /// Indicates that the catch-all query parameter was of the wrong
            /// type.
            /// </summary>
            IncorrectCatchAllType,

            /// <summary>
            /// Indicates an opening brace was found but no matching closing
            /// brace.
            /// </summary>
            MissingClosingBrace,

            /// <summary>
            /// Indicates that a query key does not specify a capture value.
            /// </summary>
            MissingQueryValue,

            /// <summary>
            /// Indicates that multiple parameters have been specified as
            /// coming from the request body.
            /// </summary>
            MultipleBodyParameters,

            /// <summary>
            /// Indicates that a parameter captured by a query parameter wasn't
            /// marked as optional.
            /// </summary>
            MustBeOptional,

            /// <summary>
            /// Indicates a query value was found that wasn't a capture.
            /// </summary>
            MustCaptureQueryValue,

            /// <summary>
            /// Indicates a parameter was not captured in the URL.
            /// </summary>
            ParameterNotFound,

            /// <summary>
            /// Indicates a brace was found that wasn't escaped.
            /// </summary>
            UnescapedBrace,

            /// <summary>
            /// Indicates a capture specifies a parameter that wasn't found.
            /// </summary>
            UnknownParameter,
        }
    }
}
