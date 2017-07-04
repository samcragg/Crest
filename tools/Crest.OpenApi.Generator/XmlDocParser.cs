// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi.Generator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <summary>
    /// Allows the parsing of generated XML documentation.
    /// </summary>
    internal class XmlDocParser
    {
        private readonly Dictionary<string, MethodDescription> members =
            new Dictionary<string, MethodDescription>(StringComparer.Ordinal);

        private readonly Dictionary<string, string> properties =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private readonly Dictionary<string, ClassDescription> types =
            new Dictionary<string, ClassDescription>(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlDocParser"/> class.
        /// </summary>
        /// <param name="stream">Contains the XML data to parse.</param>
        public XmlDocParser(Stream stream)
        {
            using (var reader = XmlReader.Create(stream))
            {
                while (reader.Read())
                {
                    if ((reader.NodeType == XmlNodeType.Element) && (reader.Name == "member"))
                    {
                        string member = reader.GetAttribute("name");
                        if (string.IsNullOrEmpty(member))
                        {
                            continue;
                        }

                        this.AddMember(reader, member);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlDocParser"/> class.
        /// </summary>
        /// <remarks>
        /// Used to allow this class to be mocked during unit tests.
        /// </remarks>
        protected XmlDocParser()
        {
        }

        /// <summary>
        /// Gets the summary for the specified type.
        /// </summary>
        /// <param name="type">The type to get the documentation for.</param>
        /// <returns>The description information for the type.</returns>
        public virtual ClassDescription GetClassDescription(Type type)
        {
            string className = FormatTypeName(type);
            this.types.TryGetValue(className, out ClassDescription summary);
            return summary ?? new ClassDescription();
        }

        /// <summary>
        /// Gets the description for the specified property.
        /// </summary>
        /// <param name="property">
        /// The property to get the documentation for.
        /// </param>
        /// <returns>
        /// The summary for the property, or null if none was found.
        /// </returns>
        public virtual string GetDescription(PropertyInfo property)
        {
            string propertyName = FormatPropertyName(property);
            this.properties.TryGetValue(propertyName, out string summary);
            return summary;
        }

        /// <summary>
        /// Gets the documentation for the specified method.
        /// </summary>
        /// <param name="method">The method to get the documentation for.</param>
        /// <returns>The description information for the method.</returns>
        internal virtual MethodDescription GetMethodDescription(MethodInfo method)
        {
            string methodName = FormatMethodName(method);
            this.members.TryGetValue(methodName, out MethodDescription value);
            return value ?? new MethodDescription();
        }

        private static void AppendTypeFullName(StringBuilder builder, Type type)
        {
            builder.Append(type.Namespace)
                   .Append('.')
                   .Append(type.Name);
        }

        private static void AppendTypeList(StringBuilder builder, IEnumerable<Type> types)
        {
            using (IEnumerator<Type> enumerator = types.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    AppendTypeFullName(builder, enumerator.Current);

                    while (enumerator.MoveNext())
                    {
                        builder.Append(',');
                        AppendTypeFullName(builder, enumerator.Current);
                    }
                }
            }
        }

        private static string FormatMethodName(MethodInfo method)
        {
            var builder = new StringBuilder("M:");
            AppendTypeFullName(builder, method.DeclaringType);
            builder.Append('.').Append(method.Name);

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length > 0)
            {
                builder.Append('(');
                AppendTypeList(builder, parameters.Select(p => p.ParameterType));
                builder.Append(')');
            }

            return builder.ToString();
        }

        private static string FormatPropertyName(PropertyInfo property)
        {
            var builder = new StringBuilder("P:");
            AppendTypeFullName(builder, property.DeclaringType);
            builder.Append('.').Append(property.Name);
            return builder.ToString();
        }

        private static string FormatTypeName(Type type)
        {
            var builder = new StringBuilder("T:");
            AppendTypeFullName(builder, type);
            return builder.ToString();
        }

        private static string GetReaderContent(XmlReader reader)
        {
            string innerXml = reader.ReadInnerXml();

            // Replace all whitespace with a single space (allows for spaces
            // before and after a newline to be turned into a single space).
            return Regex.Replace(innerXml, "\\s+", " ").Trim();
        }

        private static ClassDescription ParseClassDocumentation(XmlReader reader)
        {
            var description = new ClassDescription();
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "remarks":
                            description.Remarks = GetReaderContent(reader);
                            break;

                        case "summary":
                            description.Summary = GetReaderContent(reader);
                            break;
                    }
                }
            }

            return description;
        }

        private static MethodDescription ParseMethodDescription(XmlReader reader)
        {
            var description = new MethodDescription();
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "param":
                            string parameter = reader.GetAttribute("name");
                            description.Parameters[parameter] = GetReaderContent(reader);
                            break;

                        case "remarks":
                            description.Remarks = GetReaderContent(reader);
                            break;

                        case "returns":
                            description.Returns = GetReaderContent(reader);
                            break;

                        case "summary":
                            description.Summary = GetReaderContent(reader);
                            break;
                    }
                }
            }

            return description;
        }

        private static string ParsePropertyDescription(string description)
        {
            const string GetOrSets = "Gets or sets ";
            const string Gets = "Gets ";
            if (description.StartsWith(GetOrSets, StringComparison.OrdinalIgnoreCase) &&
                (description.Length > (GetOrSets.Length + 1)))
            {
                return char.ToUpper(description[GetOrSets.Length]) + description.Substring(GetOrSets.Length + 1);
            }
            else if (description.StartsWith(Gets, StringComparison.OrdinalIgnoreCase) &&
                (description.Length > (Gets.Length + 1)))
            {
                return char.ToUpper(description[Gets.Length]) + description.Substring(Gets.Length + 1);
            }
            else
            {
                return description;
            }
        }

        private static string ParseSummary(XmlReader reader)
        {
            if (reader.ReadToDescendant("summary"))
            {
                return GetReaderContent(reader);
            }
            else
            {
                return string.Empty;
            }
        }

        private void AddMember(XmlReader reader, string member)
        {
            switch (member[0])
            {
                case 'M':
                    Trace.Verbose("Found method description for '{0}'", member);
                    MethodDescription methodDescription = ParseMethodDescription(reader.ReadSubtree());
                    this.members.Add(member, methodDescription);
                    break;

                case 'P':
                    Trace.Verbose("Found property description for '{0}'", member);
                    string summary = ParsePropertyDescription(ParseSummary(reader.ReadSubtree()));
                    this.properties.Add(member, summary);
                    break;

                case 'T':
                    Trace.Verbose("Found class description for '{0}'", member);
                    ClassDescription classDescription = ParseClassDocumentation(reader.ReadSubtree());
                    this.types.Add(member, classDescription);
                    break;
            }
        }
    }
}
