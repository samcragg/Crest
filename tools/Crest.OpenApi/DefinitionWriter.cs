// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Allows the writing of object definition.
    /// </summary>
    /// <remarks>
    /// https://github.com/OAI/OpenAPI-Specification/blob/master/versions/2.0.md#definitionsObject
    /// </remarks>
    internal sealed class DefinitionWriter : JsonWriter
    {
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

        /// <summary>
        /// Initializes a new instance of the <see cref="DefinitionWriter"/> class.
        /// </summary>
        /// <param name="writer">Where to write the output to.</param>
        public DefinitionWriter(TextWriter writer)
            : base(writer)
        {
        }

        /// <summary>
        /// Creates a definition entry for the specified type.
        /// </summary>
        /// <param name="type">The object type.</param>
        /// <returns>A reference to the definition entry.</returns>
        public string CreateDefinitionFor(Type type)
        {
            this.types.Add(type);
            return GetDefinitionReference(type);
        }

        /// <summary>
        /// Writes the definition section to the output.
        /// </summary>
        public void WriteDefinitions()
        {
            this.WriteRaw("\"definitions\":{");

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
                string elementType;
                if (this.TryGetPrimitive(type.GetElementType(), out elementType))
                {
                    value = "\"type\":\"array\",\"items\":{" + elementType + "}";
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
            return "#/definitions/" + type.Name;
        }

        private void WriteArray(Type elementType)
        {
            if (!this.types.Contains(elementType))
            {
                this.WriteType(elementType);
                this.Write(',');
            }

            this.WriteString(elementType.Name + "[]");
            this.WriteRaw(":{\"type\":\"array\",\"items\":{");

            string primitive;
            if (this.TryGetPrimitive(elementType, out primitive))
            {
                this.WriteRaw(primitive);
            }
            else
            {
                this.WriteRaw("\"$ref\":\"");
                this.WriteRaw(GetDefinitionReference(elementType));
                this.Write('"');
            }

            this.Write("}}");
        }

        private void WriteList<T>(IEnumerable<T> items, Action<T> writeItem)
        {
            bool first = true;
            foreach (T item in items)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    this.Write(',');
                }

                writeItem(item);
            }
        }

        private void WriteProperty(string name, PropertyInfo property)
        {
            this.WriteString(name);
            this.WriteRaw(":{");

            string primitive;
            if (this.TryGetPrimitive(property.PropertyType, out primitive))
            {
                this.WriteRaw(primitive);
            }

            this.Write('}');
        }

        private void WriteType(Type type)
        {
            var required = new SortedSet<string>();
            this.WriteString(type.Name);
            this.WriteRaw(":{\"type\":\"object\",\"properties\":{");

            this.WriteList(type.GetProperties(), property =>
            {
                string name = FormatPropertyName(property.Name);
                if (property.IsDefined(typeof(RequiredAttribute)))
                {
                    required.Add(name);
                }

                this.WriteProperty(name, property);
            });

            this.WriteRaw("},\"required\":[");
            this.WriteList(required, this.WriteString);
            this.WriteRaw("]}");
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
