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
        private CommandArgument assemblyName;
        private CommandOption outputName;
        private CommandOption xmlDocName;

        private Program(CommandLineApplication application)
        {
            this.assemblyName = application.Argument(
                "assembly",
                "The assembly to scan for routes.");

            this.outputName = application.Option(
                "-o|--output <doc>",
                "The folder to write the output to.",
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
            var application = new CommandLineApplication(throwOnUnexpectedArg: false);
            application.Name = "crest_open_api";
            application.FullName = "Crest OpenAPI documentation generator.";
            application.Description = "Generates OpenAPI JSON documentation for the Crest routes inside an assembly using information from the XML comments.";
            application.HelpOption("-?|-h|--help");

            var program = new Program(application);
            application.OnExecute(new Func<int>(program.OnExecute));

            return application.Execute(args);
        }

        private string GetOutputDirectory()
        {
            string path = this.outputName.Value();
            if (string.IsNullOrWhiteSpace(path))
            {
                return "doc";
            }
            else
            {
                return path;
            }
        }

        private XmlDocParser LoadDocumentation()
        {
            string path = this.xmlDocName.Value();
            if (string.IsNullOrWhiteSpace(path))
            {
                string assemblyPath = this.assemblyName.Value;
                path = Path.Combine(
                    Path.GetDirectoryName(assemblyPath),
                    Path.GetFileNameWithoutExtension(assemblyPath) + ".xml");
            }

            using (Stream file = File.OpenRead(path))
            {
                return new XmlDocParser(file);
            }
        }

        private int OnExecute()
        {
            try
            {
                XmlDocParser xmlDoc = this.LoadDocumentation();
                using (var loader = new AssemblyLoader(this.assemblyName.Value))
                {
                    var generator = new DocGenerator(loader.Assembly, xmlDoc);
                    generator.CreateFiles(this.GetOutputDirectory());
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("An unexpected error has occurred:");
                Console.Error.WriteLine("    " + ex.Message);
                return -1;
            }
        }
    }
}
