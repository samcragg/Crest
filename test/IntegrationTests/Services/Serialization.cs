namespace IntegrationTests.Services
{
    using System;
    using System.Threading.Tasks;
    using Crest.Core;

    public sealed class Serialization : ISerialization
    {
        public Task<LinkCollection> GetLinks(string value)
        {
            var links = new LinkCollection
            {
                { "linkName", new Uri("http://" + value) },
            };

            return Task.FromResult(links);
        }
    }
}
