﻿// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Conversion
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Converts between .NET objects and HTML
    /// </summary>
    internal partial class HtmlConverter : IContentConverter
    {
        private const string Header = "<h1>200 - OK</h1>";
        private const string HtmlMimeType = @"text/html";
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        private readonly IHtmlTemplateProvider template;

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlConverter"/> class.
        /// </summary>
        /// <param name="template">Used to provide the page template.</param>
        public HtmlConverter(IHtmlTemplateProvider template)
        {
            this.template = template;
        }

        /// <inheritdoc />
        public string ContentType
        {
            get { return HtmlMimeType; }
        }

        /// <inheritdoc />
        public IEnumerable<string> Formats
        {
            get
            {
                yield return HtmlMimeType;
                yield return "application/xhtml+xml";
            }
        }

        /// <inheritdoc />
        public int Priority
        {
            get { return 500; }
        }

        /// <inheritdoc />
        public void WriteTo(Stream stream, object obj)
        {
            using (var writer = new StreamWriter(stream, DefaultEncoding))
            {
                string htmlTemplate = this.template.Template;
                int location = this.template.ContentLocation;

                writer.Write(htmlTemplate.Substring(0, location));
                writer.Write(Header);
                writer.Write(this.template.HintText);

                writer.WriteLine("<pre><code>");
                var walker = new ObjectWalker(writer);
                walker.WriteObject(obj);
                writer.WriteLine("</code></pre>");

                writer.Write(htmlTemplate.Substring(location));
            }
        }
    }
}
