namespace Host.UnitTests.Serialization
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Xml;
    using Crest.Host.Conversion;
    using FluentAssertions;
    using Xunit;

    public class TimeSpanConverterTests
    {
        public sealed class TryReadTimeSpan : TimeSpanConverterTests
        {
            [Fact]
            public void ShouldIgnoreTheCase()
            {
                ParseResult<TimeSpan> result =
                    TimeSpanConverter.TryReadTimeSpan("p12dt3h4m5.6s".AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Value.Should().BeCloseTo(new TimeSpan(12, 3, 4, 5, 600));
            }

            [Theory]
            [InlineData("+")]
            [InlineData("P1H")]
            [InlineData("T1H")]
            [InlineData("-1H")]
            [InlineData("X1X")]
            [InlineData("12")]
            [InlineData("-PT")]
            [InlineData("P0")]
            [InlineData("0:1")]
            [InlineData("0:1:2.")]
            public void ShouldNotReadInvalidFormats(string input)
            {
                ParseResult<TimeSpan> result =
                    TimeSpanConverter.TryReadTimeSpan(input.AsSpan());

                result.IsSuccess.Should().BeFalse();
            }

            [Theory]
            [InlineData("P1Y", 365)]
            [InlineData("-P1Y", -365)]
            [InlineData("P2Y", 730)]
            [InlineData("P3Y", 1095)]
            [InlineData("P4Y", 1461)]
            [InlineData("P1M", 31)]
            [InlineData("P12M", 365)]
            [InlineData("P1.5D", 1.5)]
            [InlineData("P0Y", 0)]
            [InlineData("P0M", 0)]
            [InlineData("P0D", 0)]
            public void ShouldReadDayValues(string input, double days)
            {
                ParseResult<TimeSpan> result =
                    TimeSpanConverter.TryReadTimeSpan(input.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Length.Should().Be(input.Length);
                result.Value.TotalDays.Should().BeApproximately(days, 0.01);
            }

            [Theory]
            [InlineData("1.2:03:04.5")]
            [InlineData("1:2:3")]
            [InlineData("-1:2:3")]
            [InlineData("0:1:2.3")]
            [InlineData("0:0:0.0000001")]
            public void ShouldReadNetFormattedTimeSpans(string input)
            {
                var expected = TimeSpan.Parse(input, CultureInfo.InvariantCulture);

                ParseResult<TimeSpan> result =
                    TimeSpanConverter.TryReadTimeSpan(input.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Length.Should().Be(input.Length);
                result.Value.Should().BeCloseTo(expected);
            }

            [Theory]
            [InlineData("PT1H", 3600)]
            [InlineData("PT1M", 60)]
            [InlineData("PT1.5S", 1.5)]
            [InlineData("PT0H", 0)]
            [InlineData("PT0M", 0)]
            [InlineData("PT0S", 0)]
            public void ShouldReadTimeValues(string input, double seconds)
            {
                ParseResult<TimeSpan> result =
                    TimeSpanConverter.TryReadTimeSpan(input.AsSpan());

                result.IsSuccess.Should().BeTrue();
                result.Length.Should().Be(input.Length);
                result.Value.TotalSeconds.Should().BeApproximately(seconds, 0.01);
            }

            [Theory]
            [InlineData("12345678901:00:00")]
            public void ShouldReturnOutOfRange(string value)
            {
                ParseResult<TimeSpan> result =
                    TimeSpanConverter.TryReadTimeSpan(value.AsSpan());

                result.IsSuccess.Should().BeFalse();
                result.Error.Should().MatchEquivalentOf("*range*");
            }
        }

        public sealed class WriteTimeSpan : TimeSpanConverterTests
        {
            [Fact]
            public void ShouldIncludeDays()
            {
                string result = GetString(TimeSpan.FromDays(12));

                result.Should().BeEquivalentTo("P12D");
            }

            [Fact]
            public void ShouldIncludeFractionOfSeconds()
            {
                string result = GetString(TimeSpan.FromTicks(123));

                result.Should().BeEquivalentTo("PT0.0000123S");
            }

            [Fact]
            public void ShouldIncludeHours()
            {
                string result = GetString(TimeSpan.FromHours(12));

                result.Should().BeEquivalentTo("PT12H");
            }

            [Fact]
            public void ShouldIncludeMinutes()
            {
                string result = GetString(TimeSpan.FromMinutes(12));

                result.Should().BeEquivalentTo("PT12M");
            }

            [Fact]
            public void ShouldIncludeSeconds()
            {
                string result = GetString(TimeSpan.FromSeconds(12));

                result.Should().BeEquivalentTo("PT12S");
            }

            [Fact]
            public void ShouldNotOutputLeadingZerosForTimeParts()
            {
                string result = GetString(new TimeSpan(1, 2, 3));

                result.Should().BeEquivalentTo("PT1H2M3S");
            }

            [Theory]
            [InlineData(long.MinValue)]
            [InlineData(0)]
            [InlineData(long.MaxValue)]
            public void ShouldWriteTheLimits(long ticks)
            {
                var time = TimeSpan.FromTicks(ticks);

                string result = GetString(time);

                result.Should().BeEquivalentTo(XmlConvert.ToString(time));
            }

            private static string GetString(TimeSpan value)
            {
                byte[] buffer = new byte[TimeSpanConverter.MaximumTextLength];
                int length = TimeSpanConverter.WriteTimeSpan(buffer, 0, value);
                return Encoding.UTF8.GetString(buffer, 0, length);
            }
        }
    }
}
