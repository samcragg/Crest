// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Routing.Parsing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Parses a template URL.
    /// </summary>
    internal abstract partial class UrlParser
    {
        private readonly bool canReadBody;

        private readonly HashSet<string> capturedParameters =
            new HashSet<string>(StringComparer.Ordinal);

        private string catchAllParameter;
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
        protected abstract void OnCaptureParameter(Type parameterType, string name);

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
        /// <param name="name">The name of the captured parameter.</param>
        /// <param name="parameterType">The type of the parameter being captured.</param>
        protected abstract void OnQueryParameter(string name, Type parameterType);

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
            this.catchAllParameter = null;
            this.currentParameters = parameters.ToDictionary(p => p.Name);

            if (this.ParsePath(routeUrl))
            {
                this.CheckAllParametersAreCaptured();
            }
        }

        private static IEnumerable<(int start, int length)> FindQueryParameters(string value, int start, int end)
        {
            int separator = value.IndexOf(',', start, end - start);
            while (separator >= 0)
            {
                yield return (start, separator - start);

                start = separator + 1;
                separator = value.IndexOf(',', start, end - start);
            }

            if (start < end)
            {
                yield return (start, end - start);
            }
        }

        private bool AddBodyParameterToCaptures()
        {
            if (this.canReadBody)
            {
                ParameterData bodyParameter;

                // If the HTTP verb allows reading from the body and we have a
                // single parameter, we assume it will be read from the body without
                // having to specify [FromBody] on it
                if ((this.capturedParameters.Count == 0) && (this.currentParameters.Count == 1))
                {
                    bodyParameter = this.currentParameters.Values.First();
                }
                else if (!this.GetBodyParameter(out bodyParameter))
                {
                    return false;
                }

                if (bodyParameter != null)
                {
                    this.capturedParameters.Add(bodyParameter.Name);
                    this.OnCaptureBody(bodyParameter.ParameterType, bodyParameter.Name);
                }
            }

            return true;
        }

        private bool AddCapture(string routeUrl, int start, int end)
        {
            if (routeUrl[start] == '?')
            {
                return this.CaptureQueryParameters(routeUrl, start + 1, end);
            }
            else
            {
                string parameterName = routeUrl.Substring(start, end - start);
                ParameterData parameter = this.GetValidParameter(parameterName, start, end - start);
                if (parameter != null)
                {
                    this.OnCaptureParameter(parameter.ParameterType, parameterName);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void AddLiteral(string routeUrl, int start, int end)
        {
            int length = end - start;
            if (length > 0)
            {
                this.OnLiteralSegment(routeUrl.Substring(start, length));
            }
        }

        private bool CaptureQueryCatchAll(string name, int start, int length)
        {
            if (this.catchAllParameter != null)
            {
                this.OnError(ErrorType.MultipleCatchAllParameters, name);
                return false;
            }

            this.catchAllParameter = name;
            ParameterData parameter = this.GetValidParameter(name, start, length);
            if (parameter == null)
            {
                return false;
            }
            else if (parameter.ParameterType != typeof(object))
            {
                this.OnError(ErrorType.IncorrectCatchAllType, parameter.Name);
                return false;
            }
            else
            {
                this.OnQueryCatchAll(parameter.Name);
                return true;
            }
        }

        private bool CaptureQueryParameter(string name, int start, int end)
        {
            ParameterData parameter = this.GetValidParameter(name, start, end);
            if (parameter == null)
            {
                return false;
            }

            if (parameter.IsOptional || !parameter.ParameterType.GetTypeInfo().IsValueType)
            {
                this.OnQueryParameter(parameter.Name, parameter.ParameterType);
                return true;
            }
            else
            {
                this.OnError(ErrorType.MustBeOptional, parameter.Name);
                return false;
            }
        }

        private bool CaptureQueryParameters(string routeUrl, int start, int end)
        {
            foreach ((int index, int length) in FindQueryParameters(routeUrl, start, end))
            {
                bool captured;
                if (routeUrl[index + length - 1] == '*')
                {
                    captured = this.CaptureQueryCatchAll(
                        routeUrl.Substring(index, length - 1),
                        index,
                        length - 1);
                }
                else
                {
                    captured = this.CaptureQueryParameter(
                        routeUrl.Substring(index, length),
                        index,
                        length);
                }

                if (!captured)
                {
                    return false;
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

        private bool GetBodyParameter(out ParameterData bodyParameter)
        {
            // We've already checked that captured parameters aren't
            // marked as FromBody
            IReadOnlyList<ParameterData> bodyParameters =
                this.currentParameters.Values
                    .Where(x => x.HasBodyAttribute)
                    .ToList();

            bodyParameter = null;
            switch (bodyParameters.Count)
            {
                case 0:
                    return true;

                case 1:
                    bodyParameter = bodyParameters[0];
                    return true;

                default:
                    this.OnError(ErrorType.MultipleBodyParameters, bodyParameters[1].Name);
                    return false;
            }
        }

        private ParameterData GetValidParameter(string name, int start, int length)
        {
            if (!this.currentParameters.TryGetValue(name, out ParameterData parameter))
            {
                this.OnError(
                    ErrorType.UnknownParameter,
                    start,
                    length,
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

        private bool ParsePath(string routeUrl)
        {
            int literalStart = 0;
            int captureStart = routeUrl.IndexOf('{', 0);
            while (captureStart >= 0)
            {
                this.AddLiteral(routeUrl, literalStart, captureStart);

                int captureEnd = routeUrl.IndexOf('}', captureStart + 1);
                if (captureEnd < 0)
                {
                    this.OnError(ErrorType.MissingClosingBrace, captureStart, 1, "{");
                    return false;
                }

                if (!this.AddCapture(routeUrl, captureStart + 1, captureEnd))
                {
                    return false;
                }

                literalStart = captureEnd + 1;
                captureStart = routeUrl.IndexOf('{', literalStart);
            }

            this.AddLiteral(routeUrl, literalStart, routeUrl.Length);
            return true;
        }
    }
}
