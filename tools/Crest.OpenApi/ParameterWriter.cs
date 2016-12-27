// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Allows the writing of parameter objects.
    /// </summary>
    /// <remarks>
    /// https://github.com/OAI/OpenAPI-Specification/blob/master/versions/2.0.md#parameterObject
    /// </remarks>
    internal class ParameterWriter : JsonWriter
    {
        private const string UnknownPrimitiveType = "\"type\":\"string\"";
        private readonly DefinitionWriter definitions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterWriter"/> class.
        /// </summary>
        /// <param name="definitions">Used to write type definitions.</param>
        /// <param name="writer">Where to write the output to.</param>
        public ParameterWriter(DefinitionWriter definitions, TextWriter writer)
            : base(writer)
        {
            this.definitions = definitions;
        }

        /// <summary>
        /// Writes a parameter that is specified in the path of the URL.
        /// </summary>
        /// <param name="parameter">The parameter information.</param>
        /// <param name="description">The description of the parameter.</param>
        public void WritePathParameter(ParameterInfo parameter, string description)
        {
            this.WriteParameterStart(parameter.Name, description, "path");
            this.WriteRaw("\",\"required\":true,");
            this.WritePrimitiveType(parameter.ParameterType);
            this.Write('}');
        }

        /// <summary>
        /// Writes a parameter that is specified in the query of the URL.
        /// </summary>
        /// <param name="parameter">The parameter information.</param>
        /// <param name="description">The description of the parameter.</param>
        public void WriteQueryParameter(ParameterInfo parameter, string description)
        {
            this.WriteParameterStart(parameter.Name, description, "query");
            this.WriteRaw("\",");

            if (parameter.ParameterType == typeof(bool))
            {
                this.WriteRaw("\"type\":\"boolean\",\"allowEmptyValue\":true");
            }
            else
            {
                this.WritePrimitiveType(parameter.ParameterType);
            }

            this.WriteRaw(",\"default\":");
            this.WriteValue(GetDefaultValue(parameter));
            this.Write('}');
        }

        private static object GetDefaultValue(ParameterInfo parameter)
        {
            if (parameter.HasDefaultValue)
            {
                // Use RawDefaultValue, as it can be used in reflection only context
                return parameter.RawDefaultValue;
            }
            else
            {
                Type type = parameter.ParameterType;
                if (type.GetTypeInfo().IsValueType)
                {
                    return Activator.CreateInstance(type);
                }
                else
                {
                    return null;
                }
            }
        }

        private void WriteParameterStart(string name, string description, string type)
        {
            this.WriteRaw("{\"name\":\"");
            this.Write(name);
            this.WriteRaw("\",\"in\":\"");
            this.WriteRaw(type);
            this.WriteRaw("\",\"description\":\"");
            this.Write(description);
        }

        private void WritePrimitiveType(Type type)
        {
            string primitive;
            this.definitions.TryGetPrimitive(type, out primitive);
            this.WriteRaw(primitive ?? UnknownPrimitiveType);
        }
    }
}
