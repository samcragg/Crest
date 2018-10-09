namespace IntegrationTests.Services
{
    using System.Threading.Tasks;

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
    }
}
