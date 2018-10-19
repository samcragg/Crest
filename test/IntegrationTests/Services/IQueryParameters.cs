namespace IntegrationTests.Services
{
    using System;
    using System.Threading.Tasks;
    using Crest.Core;

    public interface IQueryParameters
    {
        [Get("/any?*={all}")]
        [Version(1)]
        Task<string> WithAny(dynamic all);

        [Get("/both?value={captured}&*={all}")]
        [Version(1)]
        Task<string> WithBoth(string captured, dynamic all);

        [Get("/captured?value={captured}")]
        [Version(1)]
        Task<string> WithCaptured(string captured);

        [Get("/queryable?*={filter}")]
        [Version(1)]
        Task<string> WithFilter(dynamic filter);
    }
}
