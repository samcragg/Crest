namespace IntegrationTests.Services
{
    using System.Threading.Tasks;
    using Crest.Core;

    public interface IVersioned
    {
        [Get("/version")]
        [Version(1, 1)]
        Task<string> GetVersion1();

        [Get("/version")]
        [Version(2)]
        Task<string> GetVersion2();
    }
}
