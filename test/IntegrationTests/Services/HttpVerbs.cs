namespace IntegrationTests.Services
{
    using System.Threading.Tasks;

    public sealed class HttpVerbs : IHttpVerbs
    {
        internal const string Endpoint = "/endpoint";

        public Task<string> DeleteMethod()
        {
            return Task.FromResult("delete");
        }

        public Task<string> GetMethod()
        {
            return Task.FromResult("get");
        }

        public Task<string> PostMethod()
        {
            return Task.FromResult("post");
        }

        public Task<string> PutMethod()
        {
            return Task.FromResult("put");
        }
    }
}
