namespace BasicExample.Logic
{
    using System.Linq;
    using System.Threading.Tasks;
    using BasicExample.Api;
    using TimeZoneInfo = System.TimeZoneInfo;

    internal sealed class Example : IExample
    {
        public Task<TimeZone> GetTimeZoneAsync(string id)
        {
            TimeZone timeZone = null;
            TimeZoneInfo timeZoneInfo = GetTimeZoneInfo(id);
            if (timeZoneInfo != null)
            {
                timeZone = new TimeZone
                {
                    DisplayName = timeZoneInfo.DisplayName,
                    Id = timeZoneInfo.Id,
                    UtcOffset = (int)timeZoneInfo.BaseUtcOffset.TotalMinutes
                };
            }

            return Task.FromResult(timeZone);
        }

        public Task<int> GetTimeZoneOffsetAsync(string id)
        {
            int offset = 0;
            TimeZoneInfo timeZoneInfo = GetTimeZoneInfo(id);
            if (timeZoneInfo != null)
            {
                offset = (int)timeZoneInfo.BaseUtcOffset.TotalMinutes;
            }

            return Task.FromResult(offset);
        }

        public Task<string[]> ListTimeZonesAsync()
        {
            return Task.FromResult(
                TimeZoneInfo.GetSystemTimeZones()
                            .Select(tz => tz.Id)
                            .ToArray());
        }

        private static TimeZoneInfo GetTimeZoneInfo(string id)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (System.TimeZoneNotFoundException)
            {
                return null;
            }
        }
    }
}
