namespace IntegrationTests.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Crest.DataAccess;

    public sealed class QueryParameters : IQueryParameters
    {
        public Task<string> WithAny(dynamic all)
        {
            string stringValue = all.stringValue;
            int intValue = all.intValue;
            return Task.FromResult(stringValue + " " + intValue);
        }

        public Task<string> WithBoth(string value, dynamic all)
        {
            string stringValue = all.stringValue;
            int intValue = all.intValue;
            return Task.FromResult(value + " " + stringValue + " " + intValue);
        }

        public Task<string> WithCaptured(string value)
        {
            return Task.FromResult(value);
        }

        public Task<string> WithFilter(object filter)
        {
            IEnumerable<int> numbers = Enumerable.Range(1, 5)
                .Select(x => new IntWrapper { Value = x})
                .AsQueryable()
                .Apply().FilterAndSort(filter)
                .Select(x => x.Value);

            return Task.FromResult(string.Join(",", numbers));
        }

        public Task<int> WithInteger(int? integer = 123)
        {
            return Task.FromResult(integer.GetValueOrDefault());
        }

        public class IntWrapper
        {
            public int Value { get; set; }
        }
    }
}
