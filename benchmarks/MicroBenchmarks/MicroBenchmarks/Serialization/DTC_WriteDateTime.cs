namespace MicroBenchmarks.Serialization
{
    using System;
    using System.Globalization;
    using System.Text;
    using BenchmarkDotNet.Attributes;
    using Crest.Host.Serialization;

    public class DTC_WriteDateTime
    {
        private readonly byte[] buffer = new byte[DateTimeConverter.MaximumTextLength];
        private DateTime date;

        [Params("0001-01-01T00:00:00", "2123-12-23T12:34:56.789")]
        public string Value { get; set; }

        [GlobalSetup]
        public void _GlobalSetup()
        {
            this.date = DateTime.Parse(
                this.Value,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.AssumeUniversal);
        }

        [Benchmark]
        public int Custom()
        {
            return DateTimeConverter.WriteDateTime(this.buffer, 0, this.date);
        }

        [Benchmark(Baseline = true)]
        public byte[] Framework()
        {
            return Encoding.UTF8.GetBytes(
                this.date.ToString("o", DateTimeFormatInfo.InvariantInfo));
        }
    }
}
