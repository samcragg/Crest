namespace MicroBenchmarks.DictionaryOfStrings
{
    using System;
    using System.Collections.Generic;
    using BenchmarkDotNet.Attributes;
    using Crest.Host;

    // This test simulates how we create a dictionary to store the parsed
    // parameters for each request (we parse them into a dictionary then
    // immediately fetch them to put in the correct order that the method to
    // invoke expects)
    [MemoryDiagnoser]
    public class DOS_CreateAddAndGet
    {
        private const string Key = "GET";
        private const string Value = "http://www.example.com/";

        [Benchmark(Baseline = true)]
        public string Dictionary()
        {
            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            dictionary.Add(Key, Value);

            return dictionary[Key];
        }

        [Benchmark]
        public string StringDictionary()
        {
            var dictionary = new StringDictionary<string>();

            dictionary.Add(Key, Value);

            return dictionary[Key];
        }
    }
}
