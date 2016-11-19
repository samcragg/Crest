namespace Hill.Api
{
    /// <summary>
    /// Contains information about a time zone.
    /// </summary>
    public sealed class TimeZone
    {
        /// <summary>
        /// Gets or sets the display name for the time zone.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the time zone identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the offset (in minutes) from UTC.
        /// </summary>
        public int UtcOffset { get; set; }
    }
}
