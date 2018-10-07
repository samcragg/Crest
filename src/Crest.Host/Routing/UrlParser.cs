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
    /// Allows the parsing of URLs.
    /// </summary>
    /// <remarks>
    /// This class is designed to allow the parsing of the route information
    /// for route mapping and also to allow the analyzer to parse the route
    /// during development and provide feedback via the IDE.
    /// </remarks>
    internal abstract partial class UrlParser
    {
        private readonly bool canReadBody;
        private readonly HashSet<string> capturedParameters =
                    new HashSet<string>(StringComparer.Ordinal);

        private IReadOnlyDictionary<string, ParameterData> currentParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlParser"/> class.
        /// </summary>
        /// <param name="canReadBody">
        /// Determines whether parameters can be read from the request body.
        /// </param>
        protected UrlParser(bool canReadBody)
        {
            this.canReadBody = canReadBody;
        }

        private enum SegmentType
        {
            Literal,
            Capture,
            PartialCapture,
            Error,
        }

        /// <summary>
        /// Gets the path segments of a URL.
        /// </summary>
        /// <param name="url">The URL to parse.</param>
        /// <returns>A sequence of markers for the parts.</returns>
        internal static (int start, int length)[] GetSegments(string url)
        {
            int pathEnd = url.IndexOf('?');
            if (pathEnd < 0)
            {
                pathEnd = url.Length;
            }

            return Split(url, 0, pathEnd, '/');
        }

        /// <summary>
        /// Called when a parameter is captured for the request body.
        /// </summary>
        /// <param name="parameterType">The type of the parameter being captured.</param>
        /// <param name="name">The name of the captured parameter.</param>
        protected abstract void OnCaptureBody(Type parameterType, string name);

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
        /// Called when a catch-all query variable is captured.
        /// </summary>
        /// <param name="name">The name of the captured parameter.</param>
        protected abstract void OnQueryCatchAll(string name);

        /// <summary>
        /// Called when a query variable is captured.
        /// </summary>
        /// <param name="key">The query key name.</param>
        /// <param name="parameterType">The type of the parameter being captured.</param>
        /// <param name="name">The name of the captured parameter.</param>
        protected abstract void OnQueryParameter(string key, Type parameterType, string name);

        /// <summary>
        /// Parses the specified URL, matching the specified parameters.
        /// </summary>
        /// <param name="routeUrl">The URL to parse.</param>
        /// <param name="parameters">The parameter information.</param>
        protected virtual void ParseUrl(
            string routeUrl,
            IEnumerable<ParameterData> parameters)
        {
            this.capturedParameters.Clear();

            this.currentParameters = parameters.ToDictionary(pd => pd.Name, StringComparer.Ordinal);

            if (this.ParsePath(routeUrl) &&
                this.ParseQuery(routeUrl))
            {
                this.CheckAllParametersAreCaptured();
            }
        }

        private static (int start, int length)[] GetKeyValues(string url)
        {
            int start = url.IndexOf('?') + 1;
            if (start <= 0)
            {
                return Array.Empty<(int, int)>();
            }
            else
            {
                return Split(url, start, url.Length, '&');
            }
        }

        private static (int start, int length)[] Split(string url, int first, int last, char separator)
        {
            // Store the start/end pairs, so add one to allow for the index at
            // the end
            Span<int> indexes = stackalloc int[(last - first) + 1];
            int index = 0;
            int start = first;
            int end = url.IndexOf(separator, start, last - start);
            while (end >= 0)
            {
                if (start != end)
                {
                    indexes[index] = start;
                    index++;
                    indexes[index] = end;
                    index++;
                }

                start = end + 1;
                end = url.IndexOf(separator, start, last - start);
            }

            if (start < last)
            {
                indexes[index] = start;
                index++;
                indexes[index] = last;
                index++;
            }

            (int start, int length)[] pairs = new (int, int)[index / 2];
            index = 0;
            for (int i = 0; i < pairs.Length; i++)
            {
                start = indexes[index++];
                pairs[i] = (start, indexes[index++] - start);
            }

            return pairs;
        }

        private bool AddBodyParameterToCaptures()
        {
            if (this.canReadBody)
            {
                ParameterData bodyParameter = null;

                // If the HTTP verb allows reading from the body and we have a
                // single parameter, we assume it will be read from the body without
                // having to specify [FromBody] on it
                if ((this.capturedParameters.Count == 0) && (this.currentParameters.Count == 1))
                {
                    bodyParameter = this.currentParameters.Values.First();
                }
                else
                {
                    // We've already checked that captured parameters aren't
                    // marked as FromBody
                    List<ParameterData> bodyParameters =
                        this.currentParameters.Values
                            .Where(x => x.HasBodyAttribute)
                            .ToList();

                    if (bodyParameters.Count > 1)
                    {
                        this.OnError(ErrorType.MultipleBodyParameters, bodyParameters[1].Name);
                        return false;
                    }

                    bodyParameter = bodyParameters.FirstOrDefault();
                }

                if (bodyParameter != null)
                {
                    this.capturedParameters.Add(bodyParameter.Name);
                    this.OnCaptureBody(bodyParameter.ParameterType, bodyParameter.Name);
                }
            }

            return true;
        }

        private void CheckAllParametersAreCaptured()
        {
            if (!this.AddBodyParameterToCaptures())
            {
                return;
            }

            foreach (ParameterData parameter in this.currentParameters.Values)
            {
                if (!this.capturedParameters.Contains(parameter.Name))
                {
                    this.OnError(ErrorType.ParameterNotFound, parameter.Name);
                    break;
                }
            }
        }

        private bool GetQueryParameterName(string url, int start, int length, out string name)
        {
            switch (this.UnescapeSegment(url, start, length, out name))
            {
                case SegmentType.Error:
                case SegmentType.PartialCapture:
                    return false;

                case SegmentType.Literal:
                    this.OnError(
                        ErrorType.MustCaptureQueryValue,
                        start,
                        length,
                        url);
                    return false;

                default:
                    return true;
            }
        }

        private ParameterData GetValidParameter(int start, int length, string name)
        {
            if (!this.currentParameters.TryGetValue(name, out ParameterData parameter))
            {
                this.OnError(
                    ErrorType.UnknownParameter,
                    start + 1,
                    length - 2,
                    name);

                return null;
            }

            if (!this.capturedParameters.Add(name))
            {
                this.OnError(ErrorType.DuplicateParameter, name);
                return null;
            }

            if (parameter.HasBodyAttribute)
            {
                this.OnError(ErrorType.CannotBeMarkedAsFromBody, name);
                return null;
            }

            return parameter;
        }

        private bool HandleQueryParameter(string key, ParameterData parameter)
        {
            if (key == "*")
            {
                if (parameter.ParameterType == typeof(object))
                {
                    this.OnQueryCatchAll(parameter.Name);
                    return true;
                }
                else
                {
                    this.OnError(ErrorType.IncorrectCatchAllType, parameter.Name);
                    return false;
                }
            }
            else
            {
                if (parameter.IsOptional)
                {
                    this.OnQueryParameter(key, parameter.ParameterType, parameter.Name);
                    return true;
                }
                else
                {
                    this.OnError(ErrorType.MustBeOptional, parameter.Name);
                    return false;
                }
            }
        }

        private bool ParseKeyValue(
            string url,
            int start,
            int length,
            out string key,
            out string parameterName)
        {
            key = null;
            parameterName = null;

            int separator = url.IndexOf('=', start, length);
            if (separator < 0)
            {
                this.OnError(
                    ErrorType.MissingQueryValue,
                    start,
                    length,
                    url);

                return false;
            }

            int keyLength = separator - start;
            if (!this.GetQueryParameterName(
                url,
                separator + 1,
                length - keyLength - 1, // -1 for the separator
                out parameterName))
            {
                return false;
            }

            key = url.Substring(start, keyLength);
            return true;
        }

        private bool ParsePath(string routeUrl)
        {
            foreach ((int start, int length) in GetSegments(routeUrl))
            {
                SegmentType type = this.UnescapeSegment(routeUrl, start, length, out string segmentValue);
                if (type == SegmentType.Capture)
                {
                    ParameterData parameter = this.GetValidParameter(
                        start,
                        length,
                        segmentValue);

                    if (parameter == null)
                    {
                        return false;
                    }

                    this.OnCaptureSegment(parameter.ParameterType, segmentValue);
                }
                else if (type == SegmentType.Literal)
                {
                    this.OnLiteralSegment(segmentValue);
                }
            }

            return true;
        }

        private bool ParseQuery(string url)
        {
            foreach ((int start, int length) in GetKeyValues(url))
            {
                if (!this.ParseKeyValue(
                    url,
                    start,
                    length,
                    out string key,
                    out string parameterName))
                {
                    return false;
                }

                // We have to pass in the location of the '{parameter}' that
                // includes braces for error reporting
                ParameterData parameter = this.GetValidParameter(
                    url.IndexOf('{', start),
                    parameterName.Length + 2,
                    parameterName);
                if (parameter == null)
                {
                    return false;
                }

                if (!this.HandleQueryParameter(key, parameter))
                {
                    return false;
                }
            }

            return true;
        }

        private SegmentType UnescapeSegment(string url, int start, int length, out string segmentValue)
        {
            segmentValue = null;

            var parser = new SegmentParser(this, url, start, start + length);
            if (parser.Type == SegmentType.PartialCapture)
            {
                this.OnError(
                    ErrorType.MissingClosingBrace,
                    start + length - 1,
                    1,
                    url);
            }
            else if (parser.Type != SegmentType.Error)
            {
                segmentValue = parser.Value;
            }

            return parser.Type;
        }
    }
}
