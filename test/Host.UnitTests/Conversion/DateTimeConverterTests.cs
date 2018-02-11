namespace Host.UnitTests.Serialization
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using Xunit;

    public class DateTimeConverterTests
    {
        public sealed class TryReadDateTime : DateTimeConverterTests
        {
            [Fact]
            public void ShouldAllowHourToBe24()
            {
                // Midnight can be specified as 00:00:00 for the beginning of
                // the day and 24:00:00 for the end of the day, meaning
                // 2010-01-01T24:00:00 is the same as 2010-01-02T00:00:00
                ParseResult<DateTime> result = DateTimeConverter.TryReadDateTime("2010-01-01T24:00:00".AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Value.Day.Should().Be(2);
                result.Value.Hour.Should().Be(0);
            }

            [Fact]
            public void ShouldIgnoreTrailingData()
            {
                const string ValidDateTime = "2010-01-01T01:02:03.1234567+0100";
                ParseResult<DateTime> result = DateTimeConverter.TryReadDateTime((ValidDateTime + " Extra").AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Length.Should().Be(ValidDateTime.Length);
            }

            [Theory]
            [InlineData(2010, 10, 12)]
            public void ShouldReadBasicFormatDates(int year, int month, int day)
            {
                string date = $"{year:d4}{month:d2}{day:d2}";
                ParseResult<DateTime> result = DateTimeConverter.TryReadDateTime(date.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Value.Year.Should().Be(year);
                result.Value.Month.Should().Be(month);
                result.Value.Day.Should().Be(day);
            }

            [Theory]
            [InlineData(23, 59, 59)]
            public void ShouldReadBasicFormatTimes(int hour, int minute, int second)
            {
                string date = $"20000101T{hour:d2}{minute:d2}{second:d2}";
                ParseResult<DateTime> result = DateTimeConverter.TryReadDateTime(date.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Value.Hour.Should().Be(hour);
                result.Value.Minute.Should().Be(minute);
                result.Value.Second.Should().Be(second);
            }

            [Theory]
            [InlineData(1, 2, 3)]
            [InlineData(2010, 10, 12)]
            public void ShouldReadExtendedDates(int year, int month, int day)
            {
                string date = $"{year:d4}-{month:d2}-{day:d2}";
                ParseResult<DateTime> result = DateTimeConverter.TryReadDateTime(date.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Value.Year.Should().Be(year);
                result.Value.Month.Should().Be(month);
                result.Value.Day.Should().Be(day);
            }

            [Theory]
            [InlineData(0, 0, 0)]
            [InlineData(1, 2, 3)]
            [InlineData(23, 59, 59)]
            public void ShouldReadExtendedTimes(int hour, int minute, int second)
            {
                string date = $"2000-01-01T{hour:d2}:{minute:d2}:{second:d2}";
                ParseResult<DateTime> result = DateTimeConverter.TryReadDateTime(date.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Value.Hour.Should().Be(hour);
                result.Value.Minute.Should().Be(minute);
                result.Value.Second.Should().Be(second);
            }

            [Theory]
            [InlineData("123", 0.123)]
            [InlineData("0000123", 0.0000123)]
            [InlineData("00001234", 0.0000123)]
            public void ShouldReadFractionsOfSeconds(string fractions, double seconds)
            {
                string date = "2000-01-01T00:00:00." + fractions;
                ParseResult<DateTime> result = DateTimeConverter.TryReadDateTime(date.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Value.TimeOfDay.TotalSeconds.Should().BeApproximately(seconds, 0.0000001);
            }

            [Theory]
            [InlineData("22:30:00+04", 18, 30)]
            [InlineData("11:30:00-0700", 18, 30)]
            [InlineData("15:00:00-03:30", 18, 30)]
            [InlineData("18:30:00Z", 18, 30)]
            public void ShouldReadTimeZoneInformation(string time, int hour, int minute)
            {
                ParseResult<DateTime> result = DateTimeConverter.TryReadDateTime(
                    ("2000-01-01T" + time).AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Value.Hour.Should().Be(hour);
                result.Value.Minute.Should().Be(minute);
            }

            [Theory]
            [InlineData("12-1-1")]
            [InlineData("2010-10-12T1:2:3")]
            [InlineData("2010-10-12T01:02:03x1234")]
            [InlineData("2010-10-12T01:02:03+1")]
            [InlineData("2010-10-12T01:02:03+123")]
            [InlineData("2000-01-01T00:00:00.")]
            [InlineData("2000-01-01T00:00:00.x")]
            public void ShouldSetErrorToInvalidFormat(string value)
            {
                ParseResult<DateTime> result = DateTimeConverter.TryReadDateTime(value.AsSpan());

                result.Error.Should().MatchEquivalentOf("*format*");
            }

            [Theory]
            [InlineData("1234-12-34")]
            [InlineData("2010-10-12T12:34:60")]
            public void ShouldSetErrorToOutOfRange(string value)
            {
                ParseResult<DateTime> result = DateTimeConverter.TryReadDateTime(value.AsSpan());

                // This is the error message that DateTime uses:
                //
                //     parameters describe an un-representable DateTime.
                result.Error.Should().MatchEquivalentOf("*un-representable*");
            }
        }

        public sealed class WriteDateTime : DateTimeConverterTests
        {
            [Theory]
            [InlineData("2000-01-02T03:04:05Z")]
            [InlineData("2000-12-13T14:15:16Z")]
            [InlineData("2000-12-13T14:15:16.1230000Z")]
            public void ShouldWriteAnIso8601DateTime(string value)
            {
                byte[] buffer = new byte[DateTimeConverter.MaximumTextLength];
                var dateTime = DateTime.Parse(
                    value,
                    DateTimeFormatInfo.InvariantInfo,
                    DateTimeStyles.AdjustToUniversal);

                int length = DateTimeConverter.WriteDateTime(buffer, 0, dateTime);

                buffer.Take(length).Should().Equal(Encoding.ASCII.GetBytes(value));
            }

            [Fact]
            public void ShouldWriteAtTheSpecifiedOffset()
            {
                byte[] buffer = new byte[DateTimeConverter.MaximumTextLength + 1];
                var dateTime = new DateTime(2017, 1, 1);

                DateTimeConverter.WriteDateTime(buffer, 1, dateTime);

                buffer.Should().StartWith(new byte[] { 0, (byte)'2' });
            }
        }
    }
}
