namespace MicroBenchmarks.Serialization
{
    using System;
    using System.Text;
    using BenchmarkDotNet.Attributes;
    using Crest.Host.Serialization;

    public class GC_WriteGuid
    {
        private readonly byte[] buffer = new byte[GuidConverter.MaximumTextLength];

        public Guid Value { get; } = new Guid("2128665A-1C32-4EC9-B306-BC916737B235");

        [Benchmark]
        public int Custom()
        {
            return GuidConverter.WriteGuid(this.buffer, 0, this.Value);
        }

        [Benchmark(Baseline = true)]
        public byte[] Framework()
        {
            return Encoding.UTF8.GetBytes(this.Value.ToString());
        }
    }
}
