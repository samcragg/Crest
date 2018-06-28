// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Crest.Abstractions;

    /// <summary>
    /// Displays the health of the service when requested.
    /// </summary>
    internal class HealthPage
    {
        private readonly ExecutingAssembly assemblyInfo;
        private readonly ProcessAdapter process;
        private readonly IHtmlTemplateProvider template;
        private readonly ITimeProvider time;

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthPage"/> class.
        /// </summary>
        /// <param name="template">Used to format the HTML data.</param>
        /// <param name="time">Used to provide the current time.</param>
        /// <param name="process">
        /// Used to provide information about the current process.
        /// </param>
        /// <param name="assemblyInfo">
        /// Used to provide information about the current assembly.
        /// </param>
        public HealthPage(
            IHtmlTemplateProvider template,
            ITimeProvider time,
            ProcessAdapter process,
            ExecutingAssembly assemblyInfo)
        {
            this.template = template;
            this.time = time;
            this.process = process;
            this.assemblyInfo = assemblyInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HealthPage"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is only used to allow the type to be mocked in unit tests.
        /// </remarks>
        protected HealthPage()
        {
        }

        /// <summary>
        /// Gets the current health information of the service and writes it,
        /// as HTML, to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to write the data to.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public virtual async Task WriteToAsync(Stream stream)
        {
            char[] htmlTempalte = this.template.Template.ToCharArray();
            int insertIndex = this.template.ContentLocation;

            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteLineAsync(htmlTempalte, 0, insertIndex).ConfigureAwait(false);
                await writer.WriteLineAsync("<h1>Service Health</h1>").ConfigureAwait(false);

                await this.WriteSummaryAsync(writer).ConfigureAwait(false);

                // TODO: Write the stats (no. of requests, av. timings)
                await this.WriteAssembliesAsync(writer).ConfigureAwait(false);

                int count = htmlTempalte.Length - insertIndex;
                await writer.WriteAsync(htmlTempalte, insertIndex, count).ConfigureAwait(false);
            }
        }

        private static async Task WriteTableRowAsync(TextWriter writer, string label, string value)
        {
            await writer.WriteAsync("<tr><td>").ConfigureAwait(false);
            await writer.WriteAsync(label).ConfigureAwait(false);
            await writer.WriteAsync("</td><td>").ConfigureAwait(false);
            await writer.WriteAsync(WebUtility.HtmlEncode(value)).ConfigureAwait(false);
            await writer.WriteLineAsync("</td></tr>").ConfigureAwait(false);
        }

        private async Task WriteAssembliesAsync(StreamWriter writer)
        {
            await writer.WriteLineAsync("<h2>Assemblies</h2><p>").ConfigureAwait(false);

            foreach (ExecutingAssembly.AssemblyInfo assembly in this.assemblyInfo.GetCompileLibraries())
            {
                await writer.WriteAsync(WebUtility.HtmlEncode(assembly.Name)).ConfigureAwait(false);
                await writer.WriteAsync(" - ").ConfigureAwait(false);
                await writer.WriteAsync(WebUtility.HtmlEncode(assembly.Version)).ConfigureAwait(false);
                await writer.WriteLineAsync("<br>").ConfigureAwait(false);
            }

            await writer.WriteAsync("</p>").ConfigureAwait(false);
        }

        private async Task WriteSummaryAsync(TextWriter writer)
        {
            string FormatBytes(long value)
            {
                var bytes = new BytesUnit();
                return bytes.Format(value);
            }

            string FormatTime(TimeSpan time)
            {
                return time.TotalHours.ToString("f0", CultureInfo.InvariantCulture) +
                       time.ToString(@"\:mm\:ss");
            }

            await writer.WriteLineAsync("<h2>Summary</h2>").ConfigureAwait(false);
            await writer.WriteLineAsync("<table>").ConfigureAwait(false);

            await WriteTableRowAsync(
                writer,
                "System time",
                this.time.GetUtc().ToString("u", CultureInfo.InvariantCulture)).ConfigureAwait(false);

            await WriteTableRowAsync(
                writer,
                "Machine name",
                Environment.MachineName).ConfigureAwait(false);

            await WriteTableRowAsync(
                writer,
                "Process uptime",
                FormatTime(this.process.UpTime)).ConfigureAwait(false);

            await WriteTableRowAsync(
                writer,
                "CPU time (application)",
                FormatTime(this.process.ApplicationCpuTime)).ConfigureAwait(false);

            await WriteTableRowAsync(
                writer,
                "CPU time (system)",
                FormatTime(this.process.SystemCpuTime)).ConfigureAwait(false);

            await WriteTableRowAsync(
                writer,
                "Memory (private)",
                FormatBytes(this.process.PrivateMemory)).ConfigureAwait(false);

            await WriteTableRowAsync(
                writer,
                "Memory (working)",
                FormatBytes(this.process.WorkingMemory)).ConfigureAwait(false);

            await writer.WriteLineAsync("</table>").ConfigureAwait(false);
        }
    }
}
