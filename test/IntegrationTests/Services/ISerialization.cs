namespace IntegrationTests.Services
{
    using System;
    using System.Threading.Tasks;
    using Crest.Core;

    public interface ISerialization
    {
        [Get("/links/{value}")]
        [Version(1)]
        Task<LinkCollection> GetLinks(string value);

        [Get("/links/to/{value}")]
        [Version(1)]
        Task<Link> GetLinkTo(string value);
    }
}
