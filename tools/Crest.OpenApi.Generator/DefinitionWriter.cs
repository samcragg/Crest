// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi.Generator
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Allows the writing of object definition.
    /// </summary>
    /// <remarks>
    /// https://github.com/OAI/OpenAPI-Specification/blob/master/versions/2.0.md#definitionsObject
    /// </remarks>
    internal sealed class DefinitionWriter : JsonWriter
    {
        private const string ArrayDeclarationStart = "\"type\":\"array\",\"items\":{";
        private const string ArrayDeclarationEnd = "}";

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

        private readonly ISet<Type> types = new SortedSet<Type>(new TypeNameComparer());
        private readonly XmlDocParser xmlDoc;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefinitionWriter"/> class.
        /// </summary>
        /// <param name="xmlDoc">Contains the parsed XML documentation.</param>
        /// <param name="writer">Where to write the output to.</param>
        public DefinitionWriter(XmlDocParser xmlDoc, TextWriter writer)
            : base(writer)
        {
            this.xmlDoc = xmlDoc;
        }

        /// <summary>
        /// Creates a definition entry for the specified type.
        /// </summary>
        /// <param name="type">The object type.</param>
        /// <returns>
        /// A schema definition with a reference to the definition entry.
        /// </returns>
        public string CreateDefinitionFor(Type type)
        {
            string prefix = string.Empty;
            string suffix = string.Empty;
            if (type.IsArray)
            {
                type = type.GetElementType();
                prefix = ArrayDeclarationStart;
                suffix = ArrayDeclarationEnd;
            }

            if (this.types.Add(type))
            {
                this.AddPropertyTypes(type);
            }

            return prefix + GetDefinitionReference(type) + suffix;
        }

        /// <summary>
        /// Writes the definition section to the output.
        /// </summary>
        public void WriteDefinitions()
        {
            this.Write('{');

            this.WriteList(this.types, type =>
            {
                if (type.IsArray)
                {
                    this.WriteArray(type.GetElementType());
                }
                else
                {
                    this.WriteType(type);
                }
            });

            this.Write('}');
        }

        /// <summary>
        /// Tries to get a definition for the specified type if it is a primitive.
        /// </summary>
        /// <param name="type">The type to generate a definition for.</param>
        /// <param name="value">
        /// If type is a primitive, will contain the definition.
        /// </param>
        /// <returns>
        /// <c>true</c> if the type is a definition and <c>value</c> contains a
        /// valid definition; otherwise, false.
        /// </returns>
        internal bool TryGetPrimitive(Type type, out string value)
        {
            if (type.IsArray && (type != typeof(byte[])))
            {
                if (this.TryGetPrimitive(type.GetElementType(), out string elementType))
                {
                    value = ArrayDeclarationStart + elementType + ArrayDeclarationEnd;
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }
            else
            {
                return this.primitives.TryGetValue(type, out value);
            }
        }

        private static string FormatPropertyName(string name)
        {
            char first = char.ToLowerInvariant(name[0]);
            return first + name.Substring(1);
        }

        private static string GetDefinitionReference(Type type)
        {
            return "\"$ref\":\"#/definitions/" + type.Name + "\"";
        }

        private void AddPropertyTypes(Type type)
        {
            foreach (PropertyInfo property in type.GetProperties())
            {
                Type propertyType = property.PropertyType;
                if (propertyType.IsArray)
                {
                    propertyType = propertyType.GetElementType();
                }

                if (this.primitives.ContainsKey(propertyType))
                {
                    continue;
                }

                if (this.types.Add(propertyType))
                {
                    this.AddPropertyTypes(propertyType);
                }
            }
        }

        private void WriteArray(Type elementType)
        {
            this.WriteRaw(ArrayDeclarationStart);
            this.WriteReferenceToTypeDefinition(elementType);
            this.WriteRaw(ArrayDeclarationEnd);
        }

        private void WriteMinMax(int min, int max, string suffix)
        {
            var buffer = new StringBuilder();
            buffer.Append(",");
            if (min > 0)
            {
                buffer.Append("\"min").Append(suffix).Append("\":").Append(min);
            }

            if (max > 0)
            {
                if (buffer.Length > 1)
                {
                    buffer.Append(',');
                }

                buffer.Append("\"max").Append(suffix).Append("\":").Append(max);
            }

            if (buffer.Length > 1)
            {
                this.WriteRaw(buffer.ToString());
            }
        }

        private void WriteProperty(string name, PropertyInfo property)
        {
            Trace.Verbose("    Writing property '{0}' as '{1}'", property.Name, name);

            this.WriteString(name);
            this.WriteRaw(":{\"description\":");
            this.WriteString(this.xmlDoc.GetDescription(property));
            this.Write(',');
            this.WriteReferenceToTypeDefinition(property.PropertyType);

            if (property.PropertyType.IsArray)
            {
                this.WritePropertyArrayAttributes(property);
            }
            else if (property.PropertyType == typeof(string))
            {
                this.WritePropertyStringAttributes(property);
            }

            this.Write('}');
        }

        private void WritePropertyArrayAttributes(PropertyInfo property)
        {
            int min = -1;
            int max = -1;

            MinLengthAttribute minLength = property.GetCustomAttribute<MinLengthAttribute>();
            if (minLength != null)
            {
                min = minLength.Length;
            }

            MaxLengthAttribute maxLength = property.GetCustomAttribute<MaxLengthAttribute>();
            if (maxLength != null)
            {
                max = maxLength.Length;
            }

            this.WriteMinMax(min, max, "Items");
        }

        private void WritePropertyStringAttributes(PropertyInfo property)
        {
            int min = -1;
            int max = -1;

            StringLengthAttribute stringLength = property.GetCustomAttribute<StringLengthAttribute>();
            if (stringLength != null)
            {
                min = stringLength.MinimumLength;
                max = stringLength.MaximumLength;
            }
            else
            {
                MaxLengthAttribute maxLength = property.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLength != null)
                {
                    max = maxLength.Length;
                }

                MinLengthAttribute minLength = property.GetCustomAttribute<MinLengthAttribute>();
                if (minLength != null)
                {
                    min = minLength.Length;
                }
            }

            this.WriteMinMax(min, max, "Length");
        }

        private void WriteReferenceToTypeDefinition(Type type)
        {
            if (type.IsArray)
            {
                this.WriteArray(type.GetElementType());
            }
            else
            {
                if (this.TryGetPrimitive(type, out string primitive))
                {
                    this.WriteRaw(primitive);
                }
                else
                {
                    this.WriteRaw(GetDefinitionReference(type));
                }
            }
        }

        private void WriteType(Type type)
        {
            Trace.Verbose("Writing '{0}'", type.FullName);

            var required = new SortedSet<string>();
            this.WriteString(type.Name);
            this.WriteRaw(":{\"type\":\"object\",\"description\":");
            this.WriteString(this.xmlDoc.GetClassDescription(type)?.Summary);

            this.WriteRaw(",\"properties\":{");
            this.WriteList(type.GetProperties(), property =>
            {
                string name = FormatPropertyName(property.Name);
                if (property.IsDefined(typeof(RequiredAttribute)))
                {
                    required.Add(name);
                }

                this.WriteProperty(name, property);
            });

            if (required.Count > 0)
            {
                this.WriteRaw("},\"required\":[");
                this.WriteList(required, this.WriteString);
                this.WriteRaw("]}");
            }
            else
            {
                this.WriteRaw("}}");
            }
        }

        private sealed class TypeNameComparer : IComparer<Type>
        {
            public int Compare(Type x, Type y)
            {
                return string.CompareOrdinal(x.Name, y.Name);
            }
        }
    }
}
