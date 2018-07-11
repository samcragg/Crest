// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi.Generator
{
    using System;
    using System.IO;
    using Microsoft.Extensions.CommandLineUtils;

    /// <summary>
    /// Contains the main entry point for the program.
    /// </summary>
    public sealed class Program
    {
        private readonly CommandLineApplication application;
        private readonly CommandArgument assemblyName;
        private readonly CommandOption outputName;
        private readonly CommandOption verbosity;
        private readonly CommandOption xmlDocName;

        private Program(CommandLineApplication application)
        {
            this.application = application;

            this.assemblyName = application.Argument(
                "assembly",
                "The assembly to scan for routes.");

            this.outputName = application.Option(
                "-o|--output <doc>",
                "The folder to write the output to.",
                CommandOptionType.SingleValue);

            this.verbosity = application.Option(
                "-v|--verbosity <level>",
                "Specifies the amount of information to display. The following verbosity levels are supported: q[uiet], m[inimal], n[ormal] and d[etailed].",
                CommandOptionType.SingleValue);

            this.xmlDocName = application.Option(
                "-x|--xmlDoc <assembly.xml>",
                "The filename of the compiler generated XML documentation file.",
                CommandOptionType.SingleValue);
        }

        /// <summary>
        /// The main entry point for the program.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>The code to return to the console.</returns>
        public static int Main(string[] args)
        {
            var cla = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "crest_open_api",
                FullName = "Crest OpenAPI documentation generator.",
                Description = "Generates OpenAPI JSON documentation for the Crest routes inside an assembly using information from the XML comments."
            };

            var program = new Program(cla);

            cla.HelpOption("-?|-h|--help");
            cla.OnExecute(new Func<int>(program.OnExecute));
            return cla.Execute(args);
        }

        private void CreateFiles(XmlDocParser xmlDoc)
        {
            string assembly = this.assemblyName.Value;
            Trace.Information("Loading assembly '{0}'", assembly);

            using (var loader = new AssemblyLoader(assembly))
            {
                var generator = new DocGenerator(loader.Assembly, xmlDoc);
                generator.CreateFiles(this.GetOutputDirectory());
            }
        }

        private string GetOutputDirectory()
        {
            string path = this.outputName.Value();
            if (string.IsNullOrWhiteSpace(path))
            {
                return Path.Combine(
                    Path.GetDirectoryName(this.assemblyName.Value),
                    "docs");
            }
            else
            {
                return path;
            }
        }

        private XmlDocParser LoadDocumentation()
        {
            string xmlDoc = this.xmlDocName.Value();
            if (string.IsNullOrWhiteSpace(xmlDoc))
            {
                string assemblyPath = this.assemblyName.Value;
                xmlDoc = Path.Combine(
                    Path.GetDirectoryName(assemblyPath),
                    Path.GetFileNameWithoutExtension(assemblyPath) + ".xml");
            }

            Trace.Information("Parsing '{0}'", xmlDoc);
            using (Stream file = File.OpenRead(xmlDoc))
            {
                return new XmlDocParser(file);
            }
        }

        private int OnExecute()
        {
            try
            {
                Trace.SetUpTrace(this.verbosity.Value());

                if (string.IsNullOrWhiteSpace(this.assemblyName.Value))
                {
                    this.application.ShowHelp();
                    return 0;
                }

                XmlDocParser xmlDoc = this.LoadDocumentation();
                this.CreateFiles(xmlDoc);

                return 0;
            }
            catch (Exception ex)
            {
                Trace.Error("An unexpected error has occurred:");
                Trace.Error("    " + ex.Message);
                return -1;
            }
        }
    }
}
