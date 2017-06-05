// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi.Generator
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// Allows the writing of the OpenAPI information.
    /// </summary>
    /// <remarks>
    /// https://github.com/OAI/OpenAPI-Specification/blob/master/versions/2.0.md#infoObject
    /// </remarks>
    internal sealed class InfoObjectWriter : JsonWriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InfoObjectWriter"/> class.
        /// </summary>
        /// <param name="writer">Where to write the output to.</param>
        public InfoObjectWriter(TextWriter writer)
            : base(writer)
        {
        }

        /// <summary>
        /// Writes the InfoObject for the specified version.
        /// </summary>
        /// <param name="version">The version number.</param>
        /// <param name="assembly">The assembly information.</param>
        public void WriteInformation(int version, Assembly assembly)
        {
            var converter = new AttributeConverter();
            foreach (Attribute attribute in assembly.GetCustomAttributes())
            {
                converter.ParseAttribute(attribute);
            }

            this.WriteTitle(assembly, converter);
            this.WriteDescription(converter);
            this.WriteLicense(converter);
            this.WriteVersion(version);
        }

        private void WriteDescription(AttributeConverter converter)
        {
            if (!string.IsNullOrWhiteSpace(converter.Description))
            {
                this.WriteRaw(",\"description\":");
                this.WriteString(converter.Description);
            }
        }

        private void WriteLicense(AttributeConverter converter)
        {
            if (string.IsNullOrWhiteSpace(converter.License))
            {
                if (!string.IsNullOrWhiteSpace(converter.LicenseUrl))
                {
                    Console.WriteLine("LicenseUrl is being ignored as no license is specified.");
                }
            }
            else
            {
                this.WriteRaw(",\"license\":{\"name\":");
                this.WriteString(converter.License);

                if (!string.IsNullOrWhiteSpace(converter.LicenseUrl))
                {
                    this.WriteRaw(",\"url\":");
                    this.WriteString(converter.LicenseUrl);
                }

                this.Write('}');
            }
        }

        private void WriteTitle(Assembly assembly, AttributeConverter converter)
        {
            this.WriteRaw("\"title\":");

            if (string.IsNullOrWhiteSpace(converter.Title))
            {
                this.WriteString(assembly.GetName().Name);
            }
            else
            {
                this.WriteString(converter.Title);
            }
        }

        private void WriteVersion(int version)
        {
            this.WriteRaw(",\"version\":");
            this.WriteString(version.ToString(NumberFormatInfo.InvariantInfo));
        }

        private class AttributeConverter
        {
            public string Description { get; private set; }

            public string License { get; private set; }

            public string LicenseUrl { get; private set; }

            public string Title { get; private set; }

            public void ParseAttribute(Attribute attribute)
            {
                var title = attribute as AssemblyTitleAttribute;
                if (title != null)
                {
                    this.Title = title.Title;
                }
                else
                {
                    var description = attribute as AssemblyDescriptionAttribute;
                    if (description != null)
                    {
                        this.Description = description.Description;
                    }
                    else
                    {
                        var metadata = attribute as AssemblyMetadataAttribute;
                        if (metadata != null)
                        {
                            this.ParseMetadata(metadata.Key, metadata.Value);
                        }
                    }
                }
            }

            private void ParseMetadata(string key, string value)
            {
                if (string.Equals(key, "License", StringComparison.OrdinalIgnoreCase))
                {
                    this.License = value;
                }
                else if (string.Equals(key, "LicenseUrl", StringComparison.OrdinalIgnoreCase))
                {
                    this.LicenseUrl = value;
                }
            }
        }
    }
}
