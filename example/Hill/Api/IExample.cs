namespace Hill.Api
{
    using System;
    using System.Threading.Tasks;
    using Crest.Core;

    /// <summary>
    /// A simple API endpoint.
    /// </summary>
    public interface IExample
    {
        /// <summary>
        /// Returns all the identifiers of time zones available on the server.
        /// </summary>
        /// <returns>The identifiers of the time zones.</returns>
        [Get("timezones")]
        [Version(1)]
        Task<string[]> ListTimeZonesAsync();

        /// <summary>
        /// Gets information about a specific time zone.
        /// </summary>
        /// <param name="id">The identifier of the time zone.</param>
        /// <returns>Information about the time zone.</returns>
        [Get("timezones/{id}")]
        [Version(1)]
        Task<TimeZone> GetTimeZoneAsync(string id);

        /// <summary>
        /// Gets the difference between the time zone and UTC.
        /// </summary>
        /// <param name="id">The identifier of the time zone.</param>
        /// <returns>The offset, in minutes, from UTC.</returns>
        [Get("timezones/{id}/offset")]
        [Version(2)]
        Task<int> GetTimeZoneOffsetAsync(string id);
    }
}
