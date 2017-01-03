// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    /// <summary>
    /// Allows the writing of a single API operation on a path.
    /// </summary>
    /// <remarks>
    /// https://github.com/OAI/OpenAPI-Specification/blob/master/versions/2.0.md#operationObject
    /// </remarks>
    internal sealed class OperationObjectWriter : JsonWriter
    {
        private readonly DefinitionWriter definitions;
        private readonly ParameterWriter parameters;

        private readonly Regex queryParameters = new Regex(
            @"(?<name>[^=]+)=\{(?<parameter>[^\}]+)\}(?:&|$)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

        private readonly TagWriter tags;
        private readonly XmlDocParser xmlDoc;

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationObjectWriter"/> class.
        /// </summary>
        /// <param name="xmlDoc">Contains the parsed XML documentation.</param>
        /// <param name="definitions">Used to write type definitions.</param>
        /// <param name="tags">Used to write tags to.</param>
        /// <param name="writer">Where to write the output to.</param>
        public OperationObjectWriter(XmlDocParser xmlDoc, DefinitionWriter definitions, TagWriter tags, TextWriter writer)
            : base(writer)
        {
            this.definitions = definitions;
            this.parameters = new ParameterWriter(definitions, writer);
            this.tags = tags;
            this.xmlDoc = xmlDoc;
        }

        /// <summary>
        /// Writes out an operation for the specified route information.
        /// </summary>
        /// <param name="route">The route URL.</param>
        /// <param name="method">The method information.</param>
        public void WriteOperation(string route, MethodInfo method)
        {
            // Contains parameter name as the key and the query parameter name
            // as the value (e.g.  /route?value={key})
            IReadOnlyDictionary<string, string> query = this.GetQueryParameters(route);
            MethodDescription documentation = this.xmlDoc.GetMethodDescription(method);

            this.Write('{');
            this.WriteMetaData(method, documentation);

            this.WriteRaw(",\"parameters\":[");
            this.WriteParameters(
                method.GetParameters(),
                documentation?.Parameters ?? new Dictionary<string, string>(),
                query);

            this.WriteRaw("],\"responses\":{");
            this.WriteResponse(method.ReturnType, documentation?.Returns);
            this.Write('}');

            if (method.IsDefined(typeof(ObsoleteAttribute)))
            {
                this.WriteRaw(",\"deprecated\":true");
            }

            this.Write('}');
        }

        private IReadOnlyDictionary<string, string> GetQueryParameters(string route)
        {
            var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
            int queryStart = route.IndexOf('?') + 1;
            if ((queryStart > 0) && (queryStart < route.Length))
            {
                foreach (Match match in this.queryParameters.Matches(route, queryStart))
                {
                    parameters[match.Groups["parameter"].Value] = match.Groups["name"].Value;
                }
            }

            return parameters;
        }

        private void WriteMetaData(MethodInfo method, MethodDescription documentation)
        {
            string tag = this.tags.CreateTag(method.DeclaringType);
            this.WriteRaw("\"tags\":[");
            this.WriteString(tag);

            string summary = documentation?.Summary ?? string.Empty;
            summary = summary.Trim().TrimEnd('.');
            this.WriteRaw("],\"summary\":");
            this.WriteString(summary);

            this.WriteRaw(",\"description\":");
            this.WriteString(documentation?.Remarks);

            string id = method.DeclaringType.Name + "." + method.Name;
            this.WriteRaw(",\"operationId\":");
            this.WriteString(id);
        }

        private void WriteParameters(
            ParameterInfo[] parameterInfos,
            IReadOnlyDictionary<string, string> parameterSummaries,
            IReadOnlyDictionary<string, string> queryParams)
        {
            for (int i = 0; i < parameterInfos.Length; i++)
            {
                ParameterInfo parameter = parameterInfos[i];
                if (i != 0)
                {
                    this.Write(',');
                }

                string summary;
                parameterSummaries.TryGetValue(parameter.Name, out summary);

                string key;
                if (queryParams.TryGetValue(parameter.Name, out key))
                {
                    this.parameters.WriteQueryParameter(parameter, key, summary);
                }
                else
                {
                    this.parameters.WritePathParameter(parameter, summary);
                }
            }
        }

        private void WriteResponse(Type returnType, string returns)
        {
            if (returnType == typeof(Task))
            {
                this.WriteRaw("\"204\":{\"description\":");
                this.WriteString(returns);
                this.Write('}');
            }
            else
            {
                // The return type must be a Task<T>
                if (!typeof(Task).IsAssignableFrom(returnType))
                {
                    throw new InvalidOperationException("Interface methods must return a type assignable to Task.");
                }

                returnType = returnType.GetGenericArguments()[0];
                this.WriteRaw("\"200\":{\"description\":");
                this.WriteString(returns);
                this.WriteRaw(",\"schema\":{");
                this.WriteSchema(returnType);
                this.WriteRaw("}}");
            }
        }

        private void WriteSchema(Type type)
        {
            string primitive;
            if (this.definitions.TryGetPrimitive(type, out primitive))
            {
                this.WriteRaw(primitive);
            }
            else
            {
                this.WriteRaw(this.definitions.CreateDefinitionFor(type));
            }
        }
    }
}
