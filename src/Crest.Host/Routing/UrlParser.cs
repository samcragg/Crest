// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Allows the parsing of URLs.
    /// </summary>
    /// <remarks>
    /// This class is designed to allow the parsing of the route information
    /// for route mapping and also to allow the analyzer to parse the route
    /// during development and provide feedback via the IDE.
    /// </remarks>
    internal abstract partial class UrlParser
    {
        private readonly HashSet<string> capturedParameters =
            new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Represents the error found during parsing.
        /// </summary>
        protected enum ErrorType
        {
            /// <summary>
            /// Indicates a parameter is captured multiple times.
            /// </summary>
            DuplicateParameter,

            /// <summary>
            /// Indicates an opening brace was found but no matching closing brace.
            /// </summary>
            MissingClosingBrace,

            /// <summary>
            /// Indicates that a query key does not specify a capture value.
            /// </summary>
            MissingQueryValue,

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

        private enum SegmentType
        {
            Literal,
            Capture,
            PartialCapture,
            Error
        }

        /// <summary>
        /// Gets the path segments of a URL.
        /// </summary>
        /// <param name="url">The URL to parse.</param>
        /// <returns>A sequence of substrings.</returns>
        internal static IEnumerable<StringSegment> GetSegments(string url)
        {
            int pathEnd = url.IndexOf('?');
            if (pathEnd < 0)
            {
                pathEnd = url.Length;
            }

            int start = 0;
            int end = url.IndexOf('/', 0, pathEnd);
            while (end != -1)
            {
                if (start != end)
                {
                    yield return new StringSegment(url, start, end);
                }

                start = end + 1;
                end = url.IndexOf('/', start, pathEnd - start);
            }

            if (pathEnd != start)
            {
                yield return new StringSegment(url, start, pathEnd);
            }
        }

        /// <summary>
        /// Parses the specified URL, matching the specified parameters.
        /// </summary>
        /// <param name="routeUrl">The URL to parse.</param>
        /// <param name="parameters">The parameter names and types.</param>
        /// <param name="optionalParameters">Contains the names of optional parameters.</param>
        internal virtual void ParseUrl(
            string routeUrl,
            IReadOnlyDictionary<string, Type> parameters,
            ISet<string> optionalParameters)
        {
            this.capturedParameters.Clear();

            if (this.ParsePath(routeUrl, parameters) &&
                this.ParseQuery(routeUrl, parameters, optionalParameters.Contains))
            {
                this.CheckAllParametersAreCaptured(parameters);
            }
        }

        /// <summary>
        /// Called when a capture segment is parsed.
        /// </summary>
        /// <param name="parameterType">The type of the parameter being captured.</param>
        /// <param name="name">The name of the captured parameter.</param>
        protected abstract void OnCaptureSegment(Type parameterType, string name);

        /// <summary>
        /// Called when there is an error parsing the URL.
        /// </summary>
        /// <param name="error">The error that was found.</param>
        /// <param name="parameter">The name of the parameter causing the error.</param>
        protected abstract void OnError(ErrorType error, string parameter);

        /// <summary>
        /// Called when there is an error parsing the URL.
        /// </summary>
        /// <param name="error">The error that was found.</param>
        /// <param name="start">The index of where the error starts.</param>
        /// <param name="length">The length of the string producing the error.</param>
        /// <param name="value">The value of the string producing the error.</param>
        protected abstract void OnError(ErrorType error, int start, int length, string value);

        /// <summary>
        /// Called when a literal segment of text is parsed.
        /// </summary>
        /// <param name="value">The literal text.</param>
        protected abstract void OnLiteralSegment(string value);

        /// <summary>
        /// Called when a query variable is captured.
        /// </summary>
        /// <param name="key">The query key name.</param>
        /// <param name="parameterType">The type of the parameter being captured.</param>
        /// <param name="name">The name of the captured parameter.</param>
        protected abstract void OnQueryParameter(string key, Type parameterType, string name);

        private static IEnumerable<StringSegment> GetKeyValues(string url)
        {
            int start = url.IndexOf('?') + 1;
            if (start > 0)
            {
                int end = url.IndexOf('&', start);
                while (end != -1)
                {
                    yield return new StringSegment(url, start, end);
                    start = end + 1;
                    end = url.IndexOf('&', start);
                }

                yield return new StringSegment(url, start, url.Length);
            }
        }

        private void CheckAllParametersAreCaptured(IReadOnlyDictionary<string, Type> parameters)
        {
            foreach (string name in parameters.Keys)
            {
                if (!this.capturedParameters.Contains(name))
                {
                    this.OnError(ErrorType.ParameterNotFound, name);
                    break;
                }
            }
        }

        private bool GetQueryParameter(
            IReadOnlyDictionary<string, Type> parameters,
            StringSegment value,
            Func<string, bool> isOptional,
            out string parameterName,
            out Type parameterType)
        {
            switch (this.UnescapeSegment(value, out parameterName))
            {
                case SegmentType.Error:
                case SegmentType.PartialCapture:
                    parameterType = null;
                    return false;

                case SegmentType.Literal:
                    this.OnError(
                        ErrorType.MustCaptureQueryValue,
                        value.Start,
                        value.Count,
                        value.String);

                    parameterType = null;
                    return false;
            }

            parameterType = this.GetValidParameter(
                parameters,
                value,
                parameterName);

            if ((parameterType != null) && isOptional(parameterName))
            {
                return true;
            }
            else
            {
                this.OnError(ErrorType.MustBeOptional, parameterName);
                return false;
            }
        }

        private Type GetValidParameter(
            IReadOnlyDictionary<string, Type> parameters,
            StringSegment declaration,
            string name)
        {
            if (!parameters.TryGetValue(name, out Type parameterType))
            {
                this.OnError(
                    ErrorType.UnknownParameter,
                    declaration.Start + 1,
                    declaration.Count - 2,
                    name);

                return null;
            }

            if (!this.capturedParameters.Add(name))
            {
                this.OnError(ErrorType.DuplicateParameter, name);
                return null;
            }

            return parameterType;
        }

        private bool ParsePath(string routeUrl, IReadOnlyDictionary<string, Type> parameters)
        {
            foreach (StringSegment segment in GetSegments(routeUrl))
            {
                SegmentType type = this.UnescapeSegment(segment, out string segmentValue);
                if (type == SegmentType.Capture)
                {
                    Type parameterType = this.GetValidParameter(
                        parameters,
                        segment,
                        segmentValue);

                    if (parameterType == null)
                    {
                        return false;
                    }

                    this.OnCaptureSegment(parameterType, segmentValue);
                }
                else if (type == SegmentType.Literal)
                {
                    this.OnLiteralSegment(segmentValue);
                }
            }

            return true;
        }

        private bool ParseQuery(string url, IReadOnlyDictionary<string, Type> parameters, Func<string, bool> isOptional)
        {
            foreach (StringSegment segment in GetKeyValues(url))
            {
                string keyValue = segment.ToString();
                int separator = keyValue.IndexOf('=');
                if (separator < 0)
                {
                    this.OnError(
                        ErrorType.MissingQueryValue,
                        segment.Start,
                        segment.Count,
                        segment.String);

                    return false;
                }

                if (!this.GetQueryParameter(
                    parameters,
                    new StringSegment(segment.String, segment.Start + separator + 1, segment.End),
                    isOptional,
                    out string parameterName,
                    out Type parameterType))
                {
                    return false;
                }

                string key = keyValue.Substring(0, separator);
                this.OnQueryParameter(key, parameterType, parameterName);
            }

            return true;
        }

        private SegmentType UnescapeSegment(StringSegment segment, out string segmentValue)
        {
            segmentValue = null;

            var parser = new SegmentParser(this, segment.String, segment.Start, segment.End);
            if (parser.Type == SegmentType.PartialCapture)
            {
                this.OnError(
                    ErrorType.MissingClosingBrace,
                    segment.End - 1,
                    1,
                    segment.String);
            }
            else if (parser.Type != SegmentType.Error)
            {
                segmentValue = parser.Value;
            }

            return parser.Type;
        }
    }
}
