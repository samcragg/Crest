namespace IntegrationTests.Services
{
    using System.Threading.Tasks;

    public sealed class Versioned : IVersioned
    {
        public Task<string> GetVersion1()
        {
            return Task.FromResult("version 1");
        }

        public Task<string> GetVersion2()
        {
            return Task.FromResult("version 2");
        }
    }
}
