namespace IntegrationTests.Services
{
    using System.Threading.Tasks;
    using Crest.Core;

    public interface IRequestBody
    {
        [Post("/file")]
        [Version(1)]
        Task<string> FileMethod(IFileData file);

        [Put("/object")]
        [Version(1)]
        Task<string> PutObject(RequestBody.BodyData data);

        [Put("/string")]
        [Version(1)]
        Task<string> PutString(string body);
    }
}
