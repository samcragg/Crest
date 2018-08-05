// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Outputs the report to a HTML table.
    /// </summary>
    internal sealed class HtmlReporter : IReporter
    {
        private static readonly IUnit EmptyUnit = new LabelUnit(string.Empty);
        private readonly StringBuilder buffer = new StringBuilder();

        /// <inheritdoc/>
        public void Dispose()
        {
            // Nothing to dispose
        }

        /// <inheritdoc/>
        public string GenerateReport()
        {
            return this.buffer.ToString();
        }

        /// <inheritdoc/>
        public void Write(string label, Counter counter, IUnit unit)
        {
            this.WriteLabel(label);

            this.buffer.Append("<table>");
            this.WriteRow("Value", counter.Value, unit);
            this.buffer.AppendLine("</table>");
        }

        /// <inheritdoc/>
        public void Write(string label, Gauge gauge, IUnit unit)
        {
            this.WriteLabel(label);

            this.buffer.Append("<table>");
            this.WriteRow("Mean", (long)gauge.Mean, unit);
            this.WriteRow("Min", gauge.Minimum, unit);
            this.WriteRow("Max", gauge.Maximum, unit);
            this.WriteRow("1 Min Average", (long)gauge.OneMinuteAverage, unit);
            this.WriteRow("5 Min Average", (long)gauge.FiveMinuteAverage, unit);
            this.WriteRow("15 Min Average", (long)gauge.FifteenMinuteAverage, unit);
            this.buffer.AppendLine("</table>");
        }

        private static string Titleize(string value)
        {
            // First change camelCase -> CamelCase
            value = char.ToUpperInvariant(value[0]) + value.Substring(1);

            // Now split the words
            IEnumerable<string> words = Regex.Matches(value, "[A-Z][a-z]+")
                .Cast<Match>()
                .Select(m => m.Value);

            // Now join them again
            return string.Join(" ", words);
        }

        private void WriteLabel(string label)
        {
            this.buffer.Append("<h3>")
                .Append(Titleize(label))
                .AppendLine("</h3>");
        }

        private void WriteRow(string label, long value, IUnit unit)
        {
            this.buffer.Append("<tr><th>")
                .Append(label)
                .Append("</th><td>")
                .Append((unit ?? EmptyUnit).Format(value))
                .Append("</td></tr>");
        }
    }
}
