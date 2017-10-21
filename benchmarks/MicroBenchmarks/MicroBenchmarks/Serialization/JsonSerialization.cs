namespace MicroBenchmarks.Serialization
{
    using System;
    using System.IO;
    using System.Text;
    using BenchmarkDotNet.Attributes;
    using Crest.Host.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class JsonSerialization
    {
        private readonly SimpleData data = new SimpleData
        {
            Array = new[] { 'A', 'B' },
            Date = DateTime.UtcNow,
            Enum = SimpleEnum.Two,
            Integer = 123,
            Nullable = -123,
            Nested = new NestedData
            {
                Long = 456
            },
            String = "Need to \"escape\" some characters",
        };

        private readonly SerializerGenerator<JsonSerializerBase> generator =
            new SerializerGenerator<JsonSerializerBase>();

        private readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        public enum SimpleEnum
        {
            None = 0,
            One = 1,
            Two = 2
        }

        [Benchmark]
        public byte[] Generator()
        {
            using (var stream = new MemoryStream())
            {
                this.generator.Serialize(stream, this.data);
                return stream.ToArray();
            }
        }

        [Benchmark(Baseline = true)]
        public byte[] Json()
        {
            string json = JsonConvert.SerializeObject(this.data, this.jsonSettings);
            return Encoding.UTF8.GetBytes(json);
        }

        public sealed class NestedData
        {
            public long Long { get; set; }
        }

        public sealed class SimpleData
        {
            public char[] Array { get; set; }

            public DateTime Date { get; set; }

            public SimpleEnum Enum { get; set; }

            public int Integer { get; set; }

            public NestedData Nested { get; set; }

            public int? Nullable { get; set; }

            public string String { get; set; }
        }
    }
}
