namespace MicroBenchmarks.DictionaryOfStrings
{
    using System;
    using System.Collections.Generic;
    using BenchmarkDotNet.Attributes;
    using Crest.Host;

    // This test simulates how we use the string dictionary to retrieve the
    // routes based on the HTTP verb
    public class DOS_DefaultHttpVerbs
    {
        private const string Value = "/route";
        private readonly Dictionary<string, string> dictionary;
        private readonly StringDictionary<string> stringDictionary;

        public DOS_DefaultHttpVerbs()
        {
            this.dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.stringDictionary = new StringDictionary<string>();

            string[] verbs = { "GET", "PUT", "POST", "DELETE", "PATCH" };
            foreach (string verb in verbs)
            {
                this.dictionary.Add(verb, Value);
                this.stringDictionary.Add(verb, Value);
            }
        }

        [Params("GET", "get", "PATCH")]
        public string Verb { get; set; }

        [Benchmark(Baseline = true)]
        public string Dictionary()
        {
            return this.dictionary[this.Verb];
        }

        [Benchmark]
        public string StringDictionary()
        {
            return this.stringDictionary[this.Verb];
        }
    }
}
