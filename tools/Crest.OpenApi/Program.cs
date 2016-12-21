// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.OpenApi
{
    using System;
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
                "-o|--output <openApi.json>",
                "The filename to write the output to.",
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
            // HACK: Easier to debug...
            args = new[] { "--help" };

            var application = new CommandLineApplication(throwOnUnexpectedArg: false);
            application.Name = "crest_open_api";
            application.FullName = "Crest OpenAPI documentation generator.";
            application.Description = "Generates OpenAPI JSON documentation for the Crest routes inside an assembly using information from the XML comments.";
            application.HelpOption("-?|-h|--help");

            var program = new Program(application);
            application.OnExecute(new Func<int>(program.OnExecute));

            return application.Execute(args);
        }

        private int OnExecute()
        {
            return 0;
        }
    }
}
