namespace IntegrationTests.Services
{
    using System;
    using System.Threading.Tasks;
    using Crest.Abstractions;
    using Crest.Core;

    public sealed class Serialization : ISerialization
    {
        private readonly ILinkProvider linkProvider;

        public Serialization(ILinkProvider linkProvider)
        {
            this.linkProvider = linkProvider;
        }

        public Task<LinkCollection> GetLinks(string value)
        {
            var links = new LinkCollection
            {
                { "linkName", new Uri("http://" + value) },
            };

            return Task.FromResult(links);
        }

        public Task<Link> GetLinkTo(string value)
        {
            Link link = this.linkProvider
                .LinkTo<ISerialization>(Relations.Self)
                .Calling(x => x.GetLinks(value))
                .Build();

            return Task.FromResult(link);
        }
    }
}
