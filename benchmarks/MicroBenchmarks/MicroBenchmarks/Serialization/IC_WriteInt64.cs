namespace MicroBenchmarks.Serialization
{
    using System.Globalization;
    using System.Text;
    using BenchmarkDotNet.Attributes;
    using Crest.Host.Serialization;

    public class IC_WriteInt64
    {
        private readonly byte[] buffer = new byte[IntegerConverter.MaximumTextLength];

        [Params(long.MinValue, int.MinValue, -123, 0, 1234, int.MaxValue, long.MaxValue)]
        public long Value { get; set; }

        [Benchmark]
        public int Custom()
        {
            return IntegerConverter.WriteInt64(this.buffer, 0, this.Value);
        }

        [Benchmark(Baseline = true)]
        public byte[] Framework()
        {
            return Encoding.UTF8.GetBytes(this.Value.ToString(NumberFormatInfo.InvariantInfo));
        }
    }
}
