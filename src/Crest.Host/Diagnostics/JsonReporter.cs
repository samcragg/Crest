// Copyright (c) Samuel Cragg.
//
// Licensed under the MIT license. See LICENSE file in the project root for
// full license information.

namespace Crest.Host.Diagnostics
{
    using System.Globalization;

    /// <summary>
    /// Allows the reporting of metric information as JSON.
    /// </summary>
    internal sealed class JsonReporter : IReporter
    {
        private readonly StringBuffer buffer = new StringBuffer();
        private bool firstProperty = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonReporter"/> class.
        /// </summary>
        public JsonReporter()
        {
            this.buffer.Append('{');
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.buffer.Dispose();
        }

        /// <inheritdoc />
        public string GenerateReport()
        {
            this.buffer.Append('}');
            return this.buffer.ToString();
        }

        /// <inheritdoc />
        public void Write(string label, Counter counter, IUnit unit)
        {
            this.WriteKeyValue(label, "{");

            this.firstProperty = true;
            this.WriteKeyValue("value", counter.Value);
            this.WriteKeyValue("unit", unit);

            this.buffer.Append('}');
        }

        /// <inheritdoc />
        public void Write(string label, Gauge gauge, IUnit unit)
        {
            this.WriteKeyValue(label, "{");

            this.firstProperty = true;
            this.WriteKeyValue("count", gauge.SampleSize);
            this.WriteKeyValue("min", gauge.Minimum);
            this.WriteKeyValue("max", gauge.Maximum);
            this.WriteKeyValue("mean", (long)gauge.Mean);
            this.WriteKeyValue("stdDev", (long)gauge.StandardDeviation);
            this.WriteKeyValue("variance", (long)gauge.Variance);
            this.WriteKeyValue("movingAv1min", (long)gauge.OneMinuteAverage);
            this.WriteKeyValue("movingAv5min", (long)gauge.FiveMinuteAverage);
            this.WriteKeyValue("movingAv15min", (long)gauge.FifteenMinuteAverage);
            this.WriteKeyValue("unit", unit);

            this.buffer.Append('}');
        }

        private void WriteKeyValue(string label, long value)
        {
            this.WriteKeyValue(label, value.ToString(NumberFormatInfo.InvariantInfo));
        }

        private void WriteKeyValue(string label, IUnit unit)
        {
            this.WriteKeyValue(label, "\"");
            this.buffer.Append(unit?.ValueDescription ?? string.Empty);
            this.buffer.Append('"');
        }

        private void WriteKeyValue(string label, string value)
        {
            this.WriteSeparator();
            this.buffer.Append('"');
            this.buffer.Append(label);
            this.buffer.Append("\":");
            this.buffer.Append(value);
        }

        private void WriteSeparator()
        {
            if (!this.firstProperty)
            {
                this.buffer.Append(',');
            }

            this.firstProperty = false;
        }
    }
}
