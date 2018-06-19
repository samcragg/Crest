namespace IntegrationTests.Services
{
    using System.Threading.Tasks;
    using Crest.Core;

    public interface IHttpVerbs
    {
        [Delete(HttpVerbs.Endpoint)]
        [Version(1)]
        Task<string> DeleteMethod();

        [Get(HttpVerbs.Endpoint)]
        [Version(1)]
        Task<string> GetMethod();

        [Post(HttpVerbs.Endpoint)]
        [Version(1)]
        Task<string> PostMethod();

        [Put(HttpVerbs.Endpoint)]
        [Version(1)]
        Task<string> PutMethod();
    }
}
