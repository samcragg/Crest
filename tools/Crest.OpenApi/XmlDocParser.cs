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
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;

    /// <summary>
    /// Allows the parsing of generated XML documentation.
    /// </summary>
    internal class XmlDocParser
    {
        private readonly Dictionary<string, string> properties =
            new Dictionary<string, string>(StringComparer.Ordinal);

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
            string summary;
            this.properties.TryGetValue(propertyName, out summary);
            return summary;
        }

        private static void AppendTypeFullName(StringBuilder builder, Type type)
        {
            builder.Append(type.Namespace)
                   .Append('.')
                   .Append(type.Name);
        }

        private static string FormatPropertyDescription(string description)
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

        private static string FormatPropertyName(PropertyInfo property)
        {
            var builder = new StringBuilder("P:");
            AppendTypeFullName(builder, property.DeclaringType);

            builder.Append('.')
                   .Append(property.Name);

            return builder.ToString();
        }

        private static string GetReaderContent(XmlReader reader)
        {
            string innerXml = reader.ReadInnerXml();

            // Replace all whitespace with a single space (allows for spaces
            // before and after a newline to be turned into a single space).
            return Regex.Replace(innerXml, "\\s+", " ").Trim();
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
                case 'P':
                    string summary = FormatPropertyDescription(ParseSummary(reader.ReadSubtree()));
                    this.properties.Add(member, summary);
                    break;
            }
        }
    }
}
