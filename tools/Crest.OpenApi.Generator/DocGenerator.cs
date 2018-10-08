﻿// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi.Generator
{
    using System.IO;
    using System.IO.Compression;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Generates the documentation for an assembly.
    /// </summary>
    internal sealed class DocGenerator
    {
        private const string OpenApiFileName = "OpenAPI.json";
        private readonly Assembly assembly;
        private readonly Encoding defaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        private readonly MethodScanner scanner;
        private readonly XmlDocParser xmlDoc;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocGenerator"/> class.
        /// </summary>
        /// <param name="assembly">The assembly containing the routes.</param>
        /// <param name="xmlDoc">The parsed documentation.</param>
        public DocGenerator(Assembly assembly, XmlDocParser xmlDoc)
        {
            this.assembly = assembly;
            this.xmlDoc = xmlDoc;
            this.scanner = new MethodScanner(assembly);
        }

        /// <summary>
        /// Generates the documentation files.
        /// </summary>
        /// <param name="outputFolder">Where to write the files.</param>
        public void CreateFiles(string outputFolder)
        {
            for (int version = this.scanner.MinimumVersion; version <= this.scanner.MaximumVersion; version++)
            {
                // Create the directory structure first
                string path = Path.Combine(outputFolder, "V" + version);
                Directory.CreateDirectory(path);

                // Then the file
                path = Path.Combine(path, OpenApiFileName);
                this.CreateJsonFile(version, path);
                CreateGZipFile(path);
            }
        }

        private static void CreateGZipFile(string path)
        {
            Trace.Information("Compressing '{0}'...", Path.GetFileName(path));

            using (Stream source = File.OpenRead(path))
            using (Stream destination = File.Create(path + ".gz"))
            using (Stream compress = new GZipStream(destination, CompressionLevel.Optimal))
            {
                source.CopyTo(compress);
            }
        }

        private void CreateJsonFile(int version, string path)
        {
            Trace.Information("Creating '{0}'", path);

            using (var file = new StreamWriter(File.Create(path), this.defaultEncoding))
            {
                var writer = new OpenApiWriter(this.xmlDoc, file, version);
                writer.WriteHeader(this.assembly);
                writer.WriteOperations(this.scanner.Routes);
                writer.WriteFooter();
            }
        }
    }
}
