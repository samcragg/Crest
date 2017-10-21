namespace MicroBenchmarks.Serialization
{
    using System;
    using System.Text;
    using System.Xml;
    using BenchmarkDotNet.Attributes;
    using Crest.Host.Serialization;

    public class TSC_WriteTimeSpan
    {
        private readonly byte[] buffer = new byte[TimeSpanConverter.MaximumTextLength];
        private TimeSpan time;

        [Params(long.MinValue, 0, 12 * TimeSpan.TicksPerMinute, long.MaxValue)]
        public long Value { get; set; }

        [GlobalSetup]
        public void _GlobalSetup()
        {
            this.time = TimeSpan.FromTicks(this.Value);
        }

        [Benchmark]
        public int Custom()
        {
            return TimeSpanConverter.WriteTimeSpan(this.buffer, 0, this.time);
        }

        [Benchmark(Baseline = true)]
        public byte[] Framework()
        {
            return Encoding.UTF8.GetBytes(XmlConvert.ToString(this.time));
        }
    }
}
