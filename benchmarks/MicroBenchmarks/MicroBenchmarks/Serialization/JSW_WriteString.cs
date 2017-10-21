namespace MicroBenchmarks.Serialization
{
    using System.IO;
    using System.Text;
    using BenchmarkDotNet.Attributes;
    using Crest.Host.Serialization;

    public class JSW_WriteString
    {
        private readonly JsonStreamWriter writer = new JsonStreamWriter(Stream.Null);
        private string value;

        [Params('a', '£', '\u4e8c')]
        public char Char { get; set; }

        [Params(10, 100, 1000)]
        public int Length { get; set; }

        [GlobalSetup]
        public void _GlobalSetup()
        {
            this.value = new string(this.Char, this.Length);
        }

        [Benchmark]
        public void Custom()
        {
            this.writer.WriteString(this.value);
        }

        [Benchmark(Baseline = true)]
        public byte[] Framework()
        {
            // Simulate going through the string char by char to test for
            // ones to escape - not exactly the same but will show the framework
            // one as faster than it would actually be so close enough
            var builder = new StringBuilder(this.value.Length);
            for (int i = 0; i < this.value.Length; i++)
            {
                builder.Append(this.value[i]);
            }

            return Encoding.UTF8.GetBytes(builder.ToString());
        }
    }
}
