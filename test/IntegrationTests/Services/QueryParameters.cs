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

        public Task<string> WithBoth(string captured, dynamic all)
        {
            string stringValue = all.stringValue;
            int intValue = all.intValue;
            return Task.FromResult(captured + " " + stringValue + " " + intValue);
        }

        public Task<string> WithCaptured(string captured)
        {
            return Task.FromResult(captured);
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

        public class IntWrapper
        {
            public int Value { get; set; }
        }
    }
}
