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
    internal abstract partial class UrlParser
    {
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
        internal virtual void ParseUrl(string routeUrl, IReadOnlyDictionary<string, Type> parameters)
        {
            var usedParameters = new HashSet<string>(StringComparer.Ordinal);
            foreach (StringSegment segment in GetSegments(routeUrl))
            {
                string segmentValue;
                SegmentType type = this.UnescapeSegment(segment, out segmentValue);

                if (type == SegmentType.Capture)
                {
                    Type parameterType = this.GetValidParameter(
                        parameters,
                        usedParameters,
                        segment,
                        segmentValue);

                    if (parameterType == null)
                    {
                        return;
                    }

                    this.OnCaptureSegment(parameterType, segmentValue);
                }
                else if (type == SegmentType.Literal)
                {
                    this.OnLiteralSegment(segmentValue);
                }
            }

            this.CheckAllParametersAreCaptured(parameters, usedParameters);
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
        /// <param name="error">The error message.</param>
        /// <param name="parameter">The name of the parameter causing the error.</param>
        protected abstract void OnError(string error, string parameter);

        /// <summary>
        /// Called when there is an error parsing the URL.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <param name="start">The index of where the error starts.</param>
        /// <param name="length">The length of the string producing the error.</param>
        protected abstract void OnError(string error, int start, int length);

        /// <summary>
        /// Called when a literal segment of text is parsed.
        /// </summary>
        /// <param name="value">The literal text.</param>
        protected abstract void OnLiteralSegment(string value);

        private void CheckAllParametersAreCaptured(IReadOnlyDictionary<string, Type> parameters, ISet<string> usedParameters)
        {
            foreach (string name in parameters.Keys)
            {
                if (!usedParameters.Contains(name))
                {
                    this.OnError("Parameter is missing from the URL.", name);
                    break;
                }
            }
        }

        private Type GetValidParameter(
            IReadOnlyDictionary<string, Type> parameters,
            ISet<string> usedParameters,
            StringSegment segment,
            string segmentValue)
        {
            Type parameterType;
            if (!parameters.TryGetValue(segmentValue, out parameterType))
            {
                this.OnError(
                    "Unable to find parameter called: " + segmentValue,
                    segment.Start + 1,
                    segment.Count - 2);

                return null;
            }

            if (!usedParameters.Add(segmentValue))
            {
                this.OnError(
                    "Parameter is captured multiple times",
                    segmentValue);

                return null;
            }

            return parameterType;
        }

        private SegmentType UnescapeSegment(StringSegment segment, out string segmentValue)
        {
            segmentValue = null;

            var parser = new SegmentParser(this, segment.String, segment.Start, segment.End);
            if (parser.Type == SegmentType.PartialCapture)
            {
                this.OnError("Missing closing brace.", segment.End - 1, 1);
            }
            else if (parser.Type != SegmentType.Error)
            {
                segmentValue = parser.Value;
            }

            return parser.Type;
        }
    }
}
