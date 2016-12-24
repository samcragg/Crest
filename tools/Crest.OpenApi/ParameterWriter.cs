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

    /// <summary>
    /// Allows the writing of parameter objects.
    /// </summary>
    /// <remarks>
    /// https://github.com/OAI/OpenAPI-Specification/blob/master/versions/2.0.md#parameterObject
    /// </remarks>
    internal class ParameterWriter : JsonWriter
    {
        private const string UnknownPrimitiveType = "\"type\":\"string\"";

        private readonly Dictionary<Type, string> primitives = new Dictionary<Type, string>
        {
            { typeof(sbyte), "\"type\":\"integer\",\"format\":\"int8\"" },
            { typeof(short), "\"type\":\"integer\",\"format\":\"int16\"" },
            { typeof(int), "\"type\":\"integer\",\"format\":\"int32\"" },
            { typeof(long), "\"type\":\"integer\",\"format\":\"int64\"" },
            { typeof(byte), "\"type\":\"integer\",\"format\":\"uint8\"" },
            { typeof(ushort), "\"type\":\"integer\",\"format\":\"uint16\"" },
            { typeof(uint), "\"type\":\"integer\",\"format\":\"uint32\"" },
            { typeof(ulong), "\"type\":\"integer\",\"format\":\"uint64\"" },
            { typeof(float), "\"type\":\"number\",\"format\":\"float\"" },
            { typeof(double), "\"type\":\"number\",\"format\":\"double\"" },
            { typeof(string), "\"type\":\"string\"" },
            { typeof(bool), "\"type\":\"boolean\"" },
            { typeof(byte[]), "\"type\":\"string\",\"format\":\"byte\"" },
            { typeof(DateTime), "\"type\":\"string\",\"format\":\"date-time\"" },
            { typeof(Guid), "\"type\":\"string\",\"format\":\"uuid\"" },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterWriter"/> class.
        /// </summary>
        /// <param name="writer">Where to write the output to.</param>
        public ParameterWriter(TextWriter writer)
            : base(writer)
        {
        }

        /// <summary>
        /// Writes a parameter that is specified in the path of the URL.
        /// </summary>
        /// <param name="paramater">The parameter information.</param>
        /// <param name="description">The description of the parameter.</param>
        public void WritePathParameter(ParameterInfo paramater, string description)
        {
            this.WriteRaw("{\"name\":\"");
            this.Write(paramater.Name);
            this.WriteRaw("\",\"in\":\"path\",");
            this.WriteRaw("\"description\":\"");
            this.Write(description);
            this.WriteRaw("\",\"required\":true,");
            this.WritePrimitiveType(paramater.ParameterType);
            this.Write('}');
        }

        private void WritePrimitiveType(Type type)
        {
            if (type.IsArray && (type != typeof(byte[])))
            {
                this.WriteRaw("\"type\":\"array\",\"items\":{");
                this.WritePrimitiveType(type.GetElementType());
                this.Write('}');
            }
            else
            {
                string typeInfo;
                this.primitives.TryGetValue(type, out typeInfo);
                this.WriteRaw(typeInfo ?? UnknownPrimitiveType);
            }
        }
    }
}
