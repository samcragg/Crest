// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi.Generator
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Allows the writing of tags.
    /// </summary>
    /// <remarks>
    /// https://github.com/OAI/OpenAPI-Specification/blob/master/versions/2.0.md#tagObject
    /// </remarks>
    internal sealed class TagWriter : JsonWriter
    {
        // The specification says "The order of the tags can be used to reflect
        // on their order by the parsing tools" so we'll keep the insertion order
        // hence a list and not dictionary.
        private readonly List<TagItem> tags = new List<TagItem>();
        private readonly XmlDocParser xmlDoc;

        /// <summary>
        /// Initializes a new instance of the <see cref="TagWriter"/> class.
        /// </summary>
        /// <param name="xmlDoc">Contains the parsed XML documentation.</param>
        /// <param name="writer">Where to write the output to.</param>
        public TagWriter(XmlDocParser xmlDoc, TextWriter writer)
            : base(writer)
        {
            this.xmlDoc = xmlDoc;
        }

        /// <summary>
        /// Creates a tag for the specified interface type.
        /// </summary>
        /// <param name="interfaceType">The type of the interface.</param>
        /// <returns>The tag name.</returns>
        public string CreateTag(Type interfaceType)
        {
            // Spec says "Each tag name in the list MUST be unique"
            string name = GetTagName(interfaceType);
            if (!this.tags.Any(i => string.Equals(i.Name, name, StringComparison.Ordinal)))
            {
                ClassDescription description = this.xmlDoc.GetClassDescription(interfaceType);
                this.tags.Add(new TagItem
                {
                    Name = name,
                    Description = description?.Summary
                });
            }

            return name;
        }

        /// <summary>
        /// Writes all the created tags to the output.
        /// </summary>
        public void WriteTags()
        {
            this.Write('[');

            for (int i = 0; i < this.tags.Count; i++)
            {
                if (i != 0)
                {
                    this.Write(',');
                }

                this.WriteRaw("{\"name\":");
                this.WriteString(this.tags[i].Name);
                this.WriteDescription(this.tags[i].Description);
                this.Write('}');
            }

            this.Write(']');
        }

        private static string GetTagName(Type type)
        {
            DescriptionAttribute description =
                type.GetTypeInfo().GetCustomAttribute<DescriptionAttribute>();
            if (description != null)
            {
                return description.Description;
            }

            string name = type.Name;
            if (name[0] == 'I')
            {
                return name.Substring(1);
            }
            else
            {
                return name;
            }
        }

        private void WriteDescription(string description)
        {
            if (!string.IsNullOrEmpty(description))
            {
                description = description.Trim();
                description = description.TrimEnd('.');
                this.WriteRaw(",\"description\":");
                this.WriteString(description);
            }
        }

        private struct TagItem
        {
            public string Description { get; set; }

            public string Name { get; set; }
        }
    }
}
