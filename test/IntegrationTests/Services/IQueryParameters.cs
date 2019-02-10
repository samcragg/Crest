namespace IntegrationTests.Services
{
    using System;
    using System.Threading.Tasks;
    using Crest.Core;

    public interface IQueryParameters
    {
        [Get("/any{?all*}")]
        [Version(1)]
        Task<string> WithAny(dynamic all);

        [Get("/both{?value,all*}")]
        [Version(1)]
        Task<string> WithBoth(string value, dynamic all);

        [Get("/captured{?value}")]
        [Version(1)]
        Task<string> WithCaptured(string value);

        [Get("/queryable{?filter*}")]
        [Version(1)]
        Task<string> WithFilter(dynamic filter);
    }
}
